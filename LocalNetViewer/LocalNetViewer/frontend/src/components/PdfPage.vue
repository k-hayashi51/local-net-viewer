<template>
  <div class="viewer-page">
    <!-- ローディングオーバーレイ -->
    <div v-if="isLoading" class="loading-overlay">
      <div class="loading-spinner"></div>
      <p class="loading-text">{{ loadingText }}</p>
    </div>

    <!-- ヘッダー -->
    <header class="header" :class="{ hidden: !showHeader }">
      <button @click="goBack" class="back-button">← 戻る</button>
      <h2 class="title">{{ itemName }}</h2>
      <div v-if="viewMode === ImageShowMode.Page" class="page-selector-header">
        <select v-model.number="currentPage" @change="onPageSelect" class="page-select-header">
          <option v-for="n in totalPages" :key="n - 1" :value="n - 1">
            {{ n }}
          </option>
        </select>
        <span class="page-total">/ {{ totalPages }}</span>
      </div>
      <button @click="toggleMenu" class="hamburger-button">
        <span class="hamburger-icon">
          <span></span>
          <span></span>
          <span></span>
        </span>
      </button>
    </header>

    <!-- ページモード時のフッター進捗バー -->
    <div v-if="viewMode === ImageShowMode.Page" class="progress-footer" :class="{ hidden: !showHeader }">
      <div class="progress-bar" @mousedown="handleProgressMouseDown" @touchstart="handleProgressTouchStart" @touchmove="handleProgressTouchMove" @touchend="handleProgressTouchEnd">
        <div class="progress-fill" :style="{ width: progressPercentage + '%' }"></div>
        <div class="progress-thumb" :style="{ left: progressPercentage + '%' }"></div>
      </div>
      <div class="progress-text">{{ currentPage + 1 }} / {{ totalPages }}</div>
    </div>

    <!-- 表示モード切替メニュー -->
    <div v-if="showMenu" class="menu-overlay" @click="toggleMenu">
      <div class="menu-content" @click.stop>
        <h3 class="menu-title">表示モード</h3>
        <button @click="changeMode(ImageShowMode.Scroll)" :class="{ active: viewMode === ImageShowMode.Scroll }" class="menu-option">📜 縦スクロール</button>
        <button @click="changeMode(ImageShowMode.Page)" :class="{ active: viewMode === ImageShowMode.Page }" class="menu-option">📖 ページ送り</button>

        <div class="menu-divider"></div>

        <h3 class="menu-title">レンダリング品質</h3>
        <button @click="changeQuality(1.5)" :class="{ active: pdfQualityScale === 1.5 }" class="menu-option quality-option">
          <span class="quality-label">標準画質</span>
          <span class="quality-desc">軽量・高速</span>
        </button>
        <button @click="changeQuality(2.5)" :class="{ active: pdfQualityScale === 2.5 }" class="menu-option quality-option">
          <span class="quality-label">高画質</span>
          <span class="quality-desc">バランス</span>
        </button>
        <button @click="changeQuality(3.0)" :class="{ active: pdfQualityScale === 3.0 }" class="menu-option quality-option">
          <span class="quality-label">超高画質</span>
          <span class="quality-desc">詳細表示</span>
        </button>
        <button @click="changeQuality(4.0)" :class="{ active: pdfQualityScale === 4.0 }" class="menu-option quality-option">
          <span class="quality-label">印刷品質</span>
          <span class="quality-desc">推奨</span>
        </button>
        <button @click="changeQuality(5.0)" :class="{ active: pdfQualityScale === 5.0 }" class="menu-option quality-option">
          <span class="quality-label">最高品質</span>
          <span class="quality-desc">メモリ大</span>
        </button>
      </div>
    </div>

    <!-- PDF表示エリア -->
    <div class="pdf-viewer">
      <!-- スクロールモード -->
      <div v-if="viewMode === ImageShowMode.Scroll" class="scroll-mode">
        <div v-for="pageNum in totalPages" :key="pageNum" class="canvas-wrapper" :style="{ minHeight: pageHeights[pageNum - 1] ? pageHeights[pageNum - 1] + 'px' : '800px' }">
          <div v-if="!renderedPages[pageNum - 1]" class="page-loading">
            <div class="loading-spinner small"></div>
          </div>
          <canvas v-show="renderedPages[pageNum - 1]" :ref="(el) => setCanvasRef(el, pageNum - 1)" class="pdf-page" @load="onPageRendered(pageNum - 1)"> </canvas>
        </div>
      </div>

      <!-- ページモード -->
      <div v-else class="page-mode" :class="{ 'has-header': showHeader }" @click="handlePageClick" @touchstart="handleTouchStart" @touchmove="handleTouchMove" @touchend="handleTouchEnd">
        <div class="page-container">
          <div v-if="!renderedPages[currentPage]" class="page-loading">
            <div class="loading-spinner"></div>
          </div>
          <canvas v-show="renderedPages[currentPage]" ref="currentPageCanvas" class="current-page"> </canvas>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import type { PDFDocumentProxy } from 'pdfjs-dist';
import { ImageShowMode } from '../types';
import { getImageShowMode, getPdfQualityScale, setPdfQualityScale } from '../services/LocalStorageService';

let pdfjsLib: typeof import('pdfjs-dist') | null = null;

const props = defineProps<{
  position: string;
}>();

const viewMode = ref<ImageShowMode>(ImageShowMode.Scroll);
const currentPage = ref(0);
const totalPages = ref(0);
const showHeader = ref(false);
const showMenu = ref(false);
const lastScrollY = ref(0);
const isLoading = ref(true);
const loadingText = ref('読み込み中...');
const itemName = ref('');
const isDraggingProgress = ref(false);

// タッチイベント用の変数
const touchStartX = ref(0);
const touchStartY = ref(0);
const touchEndX = ref(0);
const touchEndY = ref(0);
const minSwipeDistance = 50;

// PDF画質設定（高いほど高画質だがメモリ使用量が増加）
const pdfQualityScale = ref(4.0);

// 可視範囲の管理
const visiblePageRange = ref({ start: 0, end: 5 });
const pageHeights = ref<number[]>([]);

let pdfDoc: PDFDocumentProxy | null = null;
const canvasRefs = ref<HTMLCanvasElement[]>([]);
const currentPageCanvas = ref<HTMLCanvasElement | null>(null);
const renderedPages = ref<Record<number, boolean>>({});
const renderingPages = ref<Record<number, boolean>>({}); // レンダリング中のページを追跡
const renderTasks = ref<Record<number, any>>({}); // レンダリングタスクを保存

const progressPercentage = computed(() => {
  if (totalPages.value === 0) return 0;
  return ((currentPage.value + 1) / totalPages.value) * 100;
});

const setCanvasRef = (el: unknown, index: number) => {
  if (el && el instanceof HTMLCanvasElement) {
    canvasRefs.value[index] = el;
  }
};

const onPageRendered = (pageNum: number) => {
  renderedPages.value[pageNum] = true;
};

// ページのCanvasをクリアしてメモリ解放
const clearPage = (pageNum: number) => {
  // レンダリング中の場合はキャンセル
  if (renderTasks.value[pageNum]) {
    try {
      renderTasks.value[pageNum].cancel();
    } catch (e) {
      // キャンセル済みの場合は無視
    }
    delete renderTasks.value[pageNum];
  }

  renderingPages.value[pageNum] = false;

  const canvas = canvasRefs.value[pageNum];
  if (canvas) {
    const context = canvas.getContext('2d');
    if (context) {
      context.clearRect(0, 0, canvas.width, canvas.height);
    }
    canvas.width = 0;
    canvas.height = 0;
  }
  renderedPages.value[pageNum] = false;
};

// 可視範囲のページを更新
let updateVisiblePagesTimeout: number | null = null;

const updateVisiblePages = () => {
  if (viewMode.value !== ImageShowMode.Scroll) return;

  // デバウンス: 連続呼び出しを防ぐ
  if (updateVisiblePagesTimeout) {
    clearTimeout(updateVisiblePagesTimeout);
  }

  updateVisiblePagesTimeout = setTimeout(() => {
    const scrollY = window.scrollY;
    const viewportHeight = window.innerHeight;

    // 可視範囲 + 前後2ページのバッファ
    let startPage = 0;
    let endPage = 0;
    let accumulatedHeight = 0;

    for (let i = 0; i < totalPages.value; i++) {
      const pageHeight = pageHeights.value[i] || viewportHeight;

      if (accumulatedHeight + pageHeight > scrollY - viewportHeight) {
        startPage = Math.max(0, i - 2);
        break;
      }
      accumulatedHeight += pageHeight;
    }

    accumulatedHeight = 0;
    for (let i = 0; i < totalPages.value; i++) {
      const pageHeight = pageHeights.value[i] || viewportHeight;
      accumulatedHeight += pageHeight;

      if (accumulatedHeight > scrollY + viewportHeight * 2) {
        endPage = Math.min(totalPages.value - 1, i + 2);
        break;
      }
    }

    if (endPage === 0) endPage = Math.min(totalPages.value - 1, startPage + 5);

    visiblePageRange.value = { start: startPage, end: endPage };

    // 可視範囲のページのみレンダリング
    for (let i = startPage; i <= endPage; i++) {
      if (!renderedPages.value[i] && !renderingPages.value[i]) {
        renderPage(i);
      }
    }

    // 可視範囲外のページをクリア(メモリ解放)
    for (let i = 0; i < totalPages.value; i++) {
      if ((i < startPage - 3 || i > endPage + 3) && renderedPages.value[i]) {
        clearPage(i);
      }
    }
  }, 100) as unknown as number;
};

onMounted(async () => {
  viewMode.value = getImageShowMode();
  pdfQualityScale.value = getPdfQualityScale();

  await loadPDF();
  window.addEventListener('scroll', handleScroll);
  window.addEventListener('click', handleScreenClick);
  window.addEventListener('keydown', handleKeyDown);

  // ページモード時はbodyのスクロールを無効化
  if (viewMode.value === ImageShowMode.Page) {
    document.body.style.overflow = 'hidden';
    document.body.style.position = 'fixed';
    document.body.style.width = '100%';
  }
});

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll);
  window.removeEventListener('click', handleScreenClick);
  window.removeEventListener('keydown', handleKeyDown);
  document.removeEventListener('mousemove', handleProgressMouseMove);
  document.removeEventListener('mouseup', handleProgressMouseUp);

  // タイムアウトをクリア
  if (updateVisiblePagesTimeout) {
    clearTimeout(updateVisiblePagesTimeout);
  }

  // bodyのスクロール制限を解除
  document.body.style.overflow = '';
  document.body.style.position = '';
  document.body.style.width = '';

  // 全てのレンダリングタスクをキャンセル
  Object.keys(renderTasks.value).forEach((key) => {
    const pageNum = parseInt(key);
    if (renderTasks.value[pageNum]) {
      try {
        renderTasks.value[pageNum].cancel();
      } catch (e) {
        // キャンセル済みの場合は無視
      }
    }
  });

  // メモリクリーンアップ
  for (let i = 0; i < totalPages.value; i++) {
    if (renderedPages.value[i]) {
      clearPage(i);
    }
  }
});

const loadPDF = async () => {
  try {
    if (!pdfjsLib) {
      pdfjsLib = await import('pdfjs-dist');

      pdfjsLib.GlobalWorkerOptions.workerSrc = new URL('pdfjs-dist/build/pdf.worker.min.mjs', import.meta.url).toString();
    }

    // Range Requestを使用してPDFを段階的に読み込む
    const loadingTask = pdfjsLib.getDocument({
      url: `/api/files/${props.position}/pdf`,
      // Range Requestを有効化（サーバーが対応している場合）
      rangeChunkSize: 65536, // 64KB単位で取得
      disableAutoFetch: true, // 自動先読みを無効化（メモリ節約）
      disableStream: false, // ストリーミングを有効化
    });

    loadingText.value = 'PDF情報取得中...';
    pdfDoc = await loadingTask.promise;
    totalPages.value = pdfDoc.numPages;

    // ページ高さの初期化
    pageHeights.value = new Array(totalPages.value).fill(800);

    isLoading.value = false;

    // 初期表示
    if (viewMode.value === ImageShowMode.Scroll) {
      setTimeout(() => updateVisiblePages(), 100);
    } else {
      setTimeout(() => renderPage(currentPage.value), 100);
    }
  } catch (error) {
    console.error('PDF読み込みエラー:', error);
    isLoading.value = false;
  }
};

const renderPage = async (pageNum: number) => {
  if (!pdfDoc) return;

  // 既にレンダリング中の場合はスキップ
  if (renderingPages.value[pageNum]) {
    return;
  }

  // レンダリング中のタスクがあればキャンセル
  if (renderTasks.value[pageNum]) {
    try {
      renderTasks.value[pageNum].cancel();
    } catch (e) {
      // キャンセル済みの場合は無視
    }
    delete renderTasks.value[pageNum];
  }

  renderingPages.value[pageNum] = true;

  try {
    const page = await pdfDoc.getPage(pageNum + 1);
    let canvas: HTMLCanvasElement | null = null;

    if (viewMode.value === ImageShowMode.Page) {
      canvas = currentPageCanvas.value;
    } else {
      canvas = canvasRefs.value[pageNum] || null;
    }

    if (!canvas) {
      renderingPages.value[pageNum] = false;
      return;
    }

    const context = canvas.getContext('2d');
    if (!context) {
      renderingPages.value[pageNum] = false;
      return;
    }

    // デバイスピクセル比を考慮した高解像度レンダリング
    const devicePixelRatio = window.devicePixelRatio || 1;
    const viewport = page.getViewport({ scale: 1.0 });

    // 画面サイズに合わせた表示スケールを計算
    const maxWidth = window.innerWidth;
    const maxHeight = window.innerHeight;
    const scaleX = maxWidth / viewport.width;
    const scaleY = maxHeight / viewport.height;
    const displayScale = Math.min(scaleX, scaleY);

    // 高解像度レンダリング用のスケール（表示スケール × デバイスピクセル比 × 品質係数）
    const renderScale = displayScale * devicePixelRatio * pdfQualityScale.value;

    const scaledViewport = page.getViewport({ scale: renderScale });

    // Canvas要素のピクセルサイズ（高解像度）
    canvas.width = scaledViewport.width;
    canvas.height = scaledViewport.height;

    // CSS表示サイズ（画面に合わせた通常サイズ）
    const displayWidth = scaledViewport.width / devicePixelRatio / pdfQualityScale.value;
    const displayHeight = scaledViewport.height / devicePixelRatio / pdfQualityScale.value;
    canvas.style.width = `${displayWidth}px`;
    canvas.style.height = `${displayHeight}px`;

    // ページ高さを記録（スクロールモード用）
    if (viewMode.value === ImageShowMode.Scroll) {
      pageHeights.value[pageNum] = displayHeight;
    }

    const renderContext = {
      canvasContext: context,
      viewport: scaledViewport,
      canvas: canvas,
    };

    // レンダリングタスクを保存
    const renderTask = page.render(renderContext);
    renderTasks.value[pageNum] = renderTask;

    await renderTask.promise;

    // レンダリング完了後にタスクを削除
    delete renderTasks.value[pageNum];
    renderingPages.value[pageNum] = false;
    onPageRendered(pageNum);
  } catch (error: any) {
    // キャンセルエラーは無視
    if (error.name !== 'RenderingCancelledException') {
      console.error(`ページ${pageNum + 1}のレンダリングエラー:`, error);
    }
    delete renderTasks.value[pageNum];
    renderingPages.value[pageNum] = false;
  }
};

watch(currentPage, (newPage) => {
  if (viewMode.value === ImageShowMode.Page && !isLoading.value) {
    setTimeout(() => renderPage(newPage), 50);
  }
});

watch(viewMode, (newMode) => {
  if (newMode === ImageShowMode.Scroll && !isLoading.value) {
    setTimeout(() => updateVisiblePages(), 100);
  } else if (newMode === ImageShowMode.Page && !isLoading.value) {
    setTimeout(() => renderPage(currentPage.value), 100);
  }
});

const handleTouchStart = (event: TouchEvent) => {
  touchStartX.value = event.touches[0].clientX;
  touchStartY.value = event.touches[0].clientY;
};

const handleTouchMove = (event: TouchEvent) => {
  touchEndX.value = event.touches[0].clientX;
  touchEndY.value = event.touches[0].clientY;
};

const handleTouchEnd = () => {
  if (isDraggingProgress.value) {
    isDraggingProgress.value = false;
    return;
  }

  const deltaX = touchEndX.value - touchStartX.value;
  const deltaY = touchEndY.value - touchStartY.value;

  if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > minSwipeDistance) {
    if (deltaX > 0) {
      prevPage();
    } else {
      nextPage();
    }
    showHeader.value = false;
  }

  touchStartX.value = 0;
  touchStartY.value = 0;
  touchEndX.value = 0;
  touchEndY.value = 0;
};

const handleProgressMouseDown = (event: MouseEvent) => {
  isDraggingProgress.value = true;
  updateProgressPosition(event.clientX, event.currentTarget as HTMLElement);
  event.stopPropagation();
  event.preventDefault();

  document.addEventListener('mousemove', handleProgressMouseMove);
  document.addEventListener('mouseup', handleProgressMouseUp);
};

const handleProgressMouseMove = (event: MouseEvent) => {
  if (!isDraggingProgress.value) return;

  const progressBar = document.querySelector('.progress-bar') as HTMLElement;
  if (progressBar) {
    updateProgressPosition(event.clientX, progressBar);
  }
  event.preventDefault();
};

const handleProgressMouseUp = () => {
  isDraggingProgress.value = false;
  document.removeEventListener('mousemove', handleProgressMouseMove);
  document.removeEventListener('mouseup', handleProgressMouseUp);
};

const updateProgressPosition = (clientX: number, element: HTMLElement) => {
  const rect = element.getBoundingClientRect();
  const x = clientX - rect.left;
  const percentage = Math.max(0, Math.min(1, x / rect.width));
  const newPage = Math.floor(percentage * totalPages.value);
  currentPage.value = Math.max(0, Math.min(newPage, totalPages.value - 1));
};

const handleProgressTouchStart = (event: TouchEvent) => {
  isDraggingProgress.value = true;
  event.stopPropagation();
};

const handleProgressTouchMove = (event: TouchEvent) => {
  if (!isDraggingProgress.value) return;

  const target = event.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  const touchX = event.touches[0].clientX - rect.left;
  const percentage = Math.max(0, Math.min(1, touchX / rect.width));
  const newPage = Math.floor(percentage * totalPages.value);
  currentPage.value = Math.max(0, Math.min(newPage, totalPages.value - 1));

  event.stopPropagation();
  event.preventDefault();
};

const handleProgressTouchEnd = (event: TouchEvent) => {
  isDraggingProgress.value = false;
  event.stopPropagation();
};

const handleScroll = () => {
  if (viewMode.value !== ImageShowMode.Scroll) return;
  showHeader.value = false;
  lastScrollY.value = window.scrollY;

  // スクロール時に可視ページを更新
  updateVisiblePages();
};

const handleScreenClick = (event: MouseEvent) => {
  if (viewMode.value !== ImageShowMode.Scroll) return;

  const target = event.target as HTMLElement;
  if (target.closest('.header') || target.closest('.menu-overlay')) {
    return;
  }

  const screenHeight = window.innerHeight;
  const clickY = event.clientY;
  const centerThreshold = screenHeight * 0.3;

  if (clickY > centerThreshold && clickY < screenHeight - centerThreshold) {
    showHeader.value = !showHeader.value;
  }
};

const handleKeyDown = (event: KeyboardEvent) => {
  // ページモードでのみキーボード操作を有効化
  if (viewMode.value !== ImageShowMode.Page) return;

  // input要素などでフォーカスされている場合は無視
  const activeElement = document.activeElement as HTMLElement;
  if (activeElement && (activeElement.tagName === 'INPUT' || activeElement.tagName === 'SELECT' || activeElement.tagName === 'TEXTAREA')) {
    return;
  }

  if (event.key === 'ArrowLeft') {
    event.preventDefault();
    prevPage();
  } else if (event.key === 'ArrowRight') {
    event.preventDefault();
    nextPage();
  }
};

const handlePageClick = (event: MouseEvent) => {
  const target = event.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  const clickX = event.clientX - rect.left;
  const width = rect.width;

  const leftThird = width / 3;
  const rightThird = (width * 2) / 3;

  if (clickX > leftThird && clickX < rightThird) {
    showHeader.value = !showHeader.value;
  } else if (clickX < leftThird) {
    prevPage();
  } else if (clickX > rightThird) {
    nextPage();
  }
};

const prevPage = () => {
  if (currentPage.value > 0) {
    currentPage.value--;
    showHeader.value = false;
  }
};

const nextPage = () => {
  if (currentPage.value < totalPages.value - 1) {
    currentPage.value++;
    showHeader.value = false;
  }
};

const onPageSelect = () => {
  showHeader.value = false;
};

const toggleMenu = () => {
  showMenu.value = !showMenu.value;
};

const changeMode = (mode: ImageShowMode) => {
  viewMode.value = mode;
  showMenu.value = false;
  showHeader.value = false;

  if (mode === ImageShowMode.Page) {
    document.body.style.overflow = 'hidden';
    document.body.style.position = 'fixed';
    document.body.style.width = '100%';
  } else {
    document.body.style.overflow = '';
    document.body.style.position = '';
    document.body.style.width = '';
    lastScrollY.value = window.scrollY;
  }
};

const changeQuality = async (scale: number) => {
  pdfQualityScale.value = scale;
  setPdfQualityScale(scale);
  showMenu.value = false;
  isLoading.value = true;

  // 全ページクリア
  for (let i = 0; i < totalPages.value; i++) {
    if (renderedPages.value[i]) {
      clearPage(i);
    }
  }

  // 品質変更後に再レンダリング
  if (viewMode.value === ImageShowMode.Scroll) {
    updateVisiblePages();
  } else {
    await renderPage(currentPage.value);
  }

  isLoading.value = false;
};

const router = useRouter();

const goBack = () => {
  const parentPosition = props.position.split('-').slice(0, -1).join('-');
  router.push(`/${parentPosition}`);
};
</script>

<style scoped>
.viewer-page {
  min-height: 100vh;
  background: var(--bg-primary);
}

.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: var(--bg-primary);
  z-index: 1000;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 1rem;
}

.loading-spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--border);
  border-top-color: var(--accent);
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

.loading-spinner.small {
  width: 32px;
  height: 32px;
  border-width: 3px;
}

.loading-text {
  font-size: 1rem;
  color: var(--text-secondary);
  font-weight: 500;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 100;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border);
  padding: 1rem 2rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  transition: transform 0.3s ease;
}

.header.hidden {
  transform: translateY(-100%);
}

.progress-footer {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 100;
  background: var(--bg-secondary);
  border-top: 1px solid var(--border);
  padding: 0.75rem 2rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  transition: transform 0.3s ease;
}

.progress-footer.hidden {
  transform: translateY(100%);
}

.progress-bar {
  position: relative;
  width: 100%;
  height: 8px;
  background: var(--bg-card);
  border-radius: 4px;
  overflow: visible;
  cursor: pointer;
  padding: 4px 0;
  user-select: none;
}

.progress-fill {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  height: 4px;
  background: var(--accent);
  transition: width 0.1s ease;
  border-radius: 2px;
  pointer-events: none;
}

.progress-thumb {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  width: 16px;
  height: 16px;
  background: var(--accent);
  border-radius: 50%;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
  transition: left 0.1s ease;
  pointer-events: none;
  cursor: grab;
}

.progress-thumb:active {
  cursor: grabbing;
}

.progress-text {
  text-align: center;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-secondary);
}

.back-button {
  padding: 0.5rem 1rem;
  background: var(--bg-card);
  border-radius: 8px;
  font-weight: 500;
  transition: all 0.2s;
  border: none;
  cursor: pointer;
  color: inherit;
}

.back-button:hover {
  background: var(--accent);
}

.title {
  font-size: 1.25rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}

.hamburger-button {
  width: 40px;
  height: 40px;
  background: var(--bg-card);
  border-radius: 8px;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  flex-shrink: 0;
}

.hamburger-button:hover {
  background: var(--accent);
}

.hamburger-icon {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.hamburger-icon span {
  display: block;
  width: 20px;
  height: 2px;
  background: currentColor;
  border-radius: 2px;
}

.page-selector-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-shrink: 0;
}

.page-select-header {
  padding: 0.5rem 0.75rem;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: inherit;
  font-weight: 500;
  font-size: 1rem;
  cursor: pointer;
  min-width: 60px;
  text-align: center;
}

.page-select-header:hover {
  background: var(--accent);
}

.page-select-header:focus {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
}

.page-total {
  font-weight: 500;
  white-space: nowrap;
}

.menu-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 200;
  display: flex;
  align-items: flex-start;
  justify-content: flex-end;
  padding: 5rem 2rem 2rem 2rem;
}

.menu-content {
  background: var(--bg-secondary);
  border-radius: 12px;
  padding: 1.5rem;
  min-width: 250px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
}

.menu-title {
  font-size: 1rem;
  font-weight: 600;
  margin-bottom: 1rem;
  color: var(--text-secondary);
}

.menu-option {
  width: 100%;
  padding: 1rem;
  background: var(--bg-card);
  border: 2px solid transparent;
  border-radius: 8px;
  font-weight: 500;
  margin-bottom: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
  text-align: left;
  color: inherit;
}

.menu-option:hover {
  background: var(--accent);
}

.menu-option.active {
  background: var(--accent);
  border-color: var(--accent);
  box-shadow: 0 0 0 3px rgba(74, 158, 255, 0.2);
}

.menu-option:last-child {
  margin-bottom: 0;
}

.menu-divider {
  height: 1px;
  background: var(--border);
  margin: 1rem 0;
}

.quality-option {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.25rem;
}

.quality-label {
  font-weight: 600;
}

.quality-desc {
  font-size: 0.75rem;
  color: var(--text-secondary);
}

.pdf-viewer {
  min-height: 100vh;
}

.scroll-mode {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.canvas-wrapper {
  position: relative;
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.page-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-height: 200px;
  background: transparent;
}

.pdf-page {
  max-width: 100%;
  height: auto;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.page-mode {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  overflow: hidden;
  touch-action: none;
  overscroll-behavior: contain;
}

.page-mode.has-header {
  padding-top: 73px;
  padding-bottom: 73px;
}

.page-container {
  position: relative;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.current-page {
  max-width: 100%;
  max-height: 100%;
  width: auto;
  height: auto;
  pointer-events: none;
  user-select: none;
  object-fit: contain;
}

@media (max-width: 768px) {
  .header {
    padding: 1rem;
  }

  .title {
    font-size: 1rem;
  }

  .progress-footer {
    padding: 0.5rem 1rem;
  }

  .page-mode.has-header {
    padding-top: 57px;
    padding-bottom: 57px;
  }

  .menu-overlay {
    padding: 4rem 1rem 1rem 1rem;
  }

  .page-selector-header {
    gap: 0.25rem;
  }

  .page-select-header {
    font-size: 0.875rem;
    padding: 0.375rem 0.5rem;
    min-width: 50px;
  }

  .page-total {
    font-size: 0.875rem;
  }
}
</style>
