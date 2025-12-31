<template>
  <div class="viewer-page">
    <!-- ローディングオーバーレイ -->
    <div v-if="isLoading" class="loading-overlay">
      <div class="loading-spinner"></div>
      <p class="loading-text">読み込み中...</p>
    </div>

    <!-- 画面上部ヘッダー（スクロール／ページ共通） -->
    <!-- showHeader=false のときは transform で画面外へ隠れる -->
    <header class="header" :class="{ hidden: !showHeader }">
      <!-- 戻るボタン：親ディレクトリへ遷移 -->
      <button @click="goBack" class="back-button">← 戻る</button>

      <!-- 現在表示中アイテム名 -->
      <h2 class="title">{{ itemName }}</h2>

      <!-- ページモード時のみ表示されるページセレクタ -->
      <div v-if="viewMode === ImageShowMode.Page" class="page-selector-header">
        <!-- ページ番号を直接選択 -->
        <select v-model.number="currentPage" @change="onPageSelect" class="page-select-header">
          <option v-for="(_, index) in imagePositions" :key="index" :value="index">
            {{ index + 1 }}
          </option>
        </select>
        <span class="page-total">/ {{ imagePositions.length }}</span>
      </div>

      <!-- ハンバーガーメニュー（表示モード切替） -->
      <button @click="toggleMenu" class="hamburger-button">
        <span class="hamburger-icon"> <span></span><span></span><span></span> </span>
      </button>
    </header>

    <!-- ページモード時のフッター進捗バー -->
    <div v-if="viewMode === ImageShowMode.Page" class="progress-footer" :class="{ hidden: !showHeader }">
      <!-- 進捗バー本体（マウス／タッチ対応） -->
      <div class="progress-bar" @mousedown="handleProgressMouseDown" @touchstart="handleProgressTouchStart" @touchmove="handleProgressTouchMove" @touchend="handleProgressTouchEnd">
        <!-- 現在位置の塗りつぶし -->
        <div class="progress-fill" :style="{ width: progressPercentage + '%' }"></div>

        <!-- ドラッグ用のつまみ -->
        <div class="progress-thumb" :style="{ left: progressPercentage + '%' }"></div>
      </div>

      <!-- ページ番号表示 -->
      <div class="progress-text">{{ currentPage + 1 }} / {{ imagePositions.length }}</div>
    </div>

    <!-- 表示モード切替メニュー -->
    <div v-if="showMenu" class="menu-overlay" @click="toggleMenu">
      <div class="menu-content" @click.stop>
        <h3 class="menu-title">表示モード</h3>

        <!-- 縦スクロールモード -->
        <button @click="changeMode(ImageShowMode.Scroll)" :class="{ active: viewMode === ImageShowMode.Scroll }" class="menu-option">📜 縦スクロール</button>

        <!-- ページ送りモード -->
        <button @click="changeMode(ImageShowMode.Page)" :class="{ active: viewMode === ImageShowMode.Page }" class="menu-option">📖 ページ送り</button>
      </div>
    </div>

    <!-- 画像表示エリア -->
    <div class="image-viewer">
      <!-- スクロールモード：全画像を縦に並べて表示 -->
      <div v-if="viewMode === ImageShowMode.Scroll" class="scroll-mode">
        <div v-for="(imagePosition, index) in imagePositions" :key="index" class="image-wrapper">
          <div v-if="!loadedImages[imagePosition]" class="image-loading">
            <div class="loading-spinner small"></div>
          </div>
          <img v-show="loadedImages[imagePosition]" :src="imageDataUrls[imagePosition]" :alt="`Page ${index + 1}`" class="page-image" @load="onImageLoad(imagePosition)" />
        </div>
      </div>

      <!-- ページモード：1枚ずつ表示 -->
      <div v-else class="page-mode" :class="{ 'has-header': showHeader }" @click="handlePageClick" @touchstart="handleTouchStart" @touchmove="handleTouchMove" @touchend="handleTouchEnd">
        <div v-if="imagePositions.length > 0" class="page-container">
          <div v-if="!loadedImages[imagePositions[currentPage]]" class="image-loading">
            <div class="loading-spinner"></div>
          </div>
          <img
            v-show="loadedImages[imagePositions[currentPage]]"
            :src="imageDataUrls[imagePositions[currentPage]]"
            :alt="`Page ${currentPage + 1}`"
            class="current-page"
            @load="onImageLoad(imagePositions[currentPage])"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { FileInfoViewModel, FileType, ImageShowMode } from '../types';
import { getImageShowMode, setImageShowMode, setPosition } from '../services/LocalStorageService';

const props = defineProps<{
  position: string;
}>();

const viewMode = ref<ImageShowMode>(ImageShowMode.Scroll);
const currentPage = ref(0);
const imagePositions = ref<string[]>([]);
const showHeader = ref(false);
const showMenu = ref(false);
const lastScrollY = ref(0);

// タッチイベント用の変数
const touchStartX = ref(0);
const touchStartY = ref(0);
const touchEndX = ref(0);
const touchEndY = ref(0);
const minSwipeDistance = 50;
const isDraggingProgress = ref(false);

// 画像ロード管理用の変数
const isLoading = ref(true);
const imageDataUrls = ref<Record<string, string>>({});
const loadedImages = ref<Record<string, boolean>>({});

const progressPercentage = computed(() => {
  if (imagePositions.value.length === 0) return 0;
  return ((currentPage.value + 1) / imagePositions.value.length) * 100;
});

const fetchImage = async (position: string): Promise<string> => {
  if (imageDataUrls.value[position]) {
    return imageDataUrls.value[position];
  }

  const response = await fetch(`/api/files/${position}`);
  const blob = await response.blob();
  const dataUrl = URL.createObjectURL(blob);
  imageDataUrls.value[position] = dataUrl;
  return dataUrl;
};

const onImageLoad = (position: string) => {
  loadedImages.value[position] = true;
};

const preloadImages = async () => {
  // 初期表示用の画像を先にロード
  if (viewMode.value === ImageShowMode.Page && imagePositions.value.length > 0) {
    await fetchImage(imagePositions.value[currentPage.value]);

    // 前後の画像をプリロード
    if (currentPage.value > 0) {
      fetchImage(imagePositions.value[currentPage.value - 1]);
    }
    if (currentPage.value < imagePositions.value.length - 1) {
      fetchImage(imagePositions.value[currentPage.value + 1]);
    }
  } else if (viewMode.value === ImageShowMode.Scroll) {
    // スクロールモードでは最初の数枚をロード
    const initialLoadCount = Math.min(3, imagePositions.value.length);
    for (let i = 0; i < initialLoadCount; i++) {
      await fetchImage(imagePositions.value[i]);
    }

    // 残りは遅延ロード
    setTimeout(() => {
      for (let i = initialLoadCount; i < imagePositions.value.length; i++) {
        fetchImage(imagePositions.value[i]);
      }
    }, 100);
  }

  isLoading.value = false;
};

onMounted(async () => {
  viewMode.value = getImageShowMode();

  const parentPosition = props.position.split('-').slice(0, -1).join('-');
  const response = await fetch(`/api/files/${parentPosition}/child`);
  const fileInfoList = (await response.json()) as FileInfoViewModel[];
  imagePositions.value = fileInfoList.filter((x) => x.fileType === FileType.Image).map((x) => x.position);

  // 初期表示位置の設定
  const initialIndex = imagePositions.value.findIndex((pos) => pos === props.position);
  if (initialIndex !== -1) {
    currentPage.value = initialIndex;
  }

  // 画像のプリロード
  await preloadImages();

  // スクロールモードの場合、該当画像までスクロール
  if (viewMode.value === ImageShowMode.Scroll && initialIndex !== -1) {
    setTimeout(() => {
      const imageWrappers = document.querySelectorAll('.image-wrapper');
      if (imageWrappers[initialIndex]) {
        imageWrappers[initialIndex].scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    }, 100);
  }

  window.addEventListener('scroll', handleScroll);
  window.addEventListener('click', handleScreenClick);
  window.addEventListener('keydown', handleKeyDown);

  // ページモード時はbodyのスクロールを無効化
  if (viewMode.value === ImageShowMode.Page) {
    lockScroll();
  }

  setPosition(parentPosition);
});

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll);
  window.removeEventListener('click', handleScreenClick);
  window.removeEventListener('keydown', handleKeyDown);
  document.removeEventListener('mousemove', handleProgressMouseMove);
  document.removeEventListener('mouseup', handleProgressMouseUp);

  // bodyのスクロール制限を解除
  unlockScroll();

  // データURLのクリーンアップ
  Object.values(imageDataUrls.value).forEach((url) => {
    URL.revokeObjectURL(url);
  });
});

// Safari対応のスクロールロック関数
const lockScroll = () => {
  const scrollY = window.scrollY;
  document.body.style.position = 'fixed';
  document.body.style.top = `-${scrollY}px`;
  document.body.style.left = '0';
  document.body.style.right = '0';
  document.body.style.width = '100%';
  document.body.style.overflow = 'hidden';
  // iOS Safari対応
  document.documentElement.style.overflow = 'hidden';
};

const unlockScroll = () => {
  const scrollY = document.body.style.top;
  document.body.style.position = '';
  document.body.style.top = '';
  document.body.style.left = '';
  document.body.style.right = '';
  document.body.style.width = '';
  document.body.style.overflow = '';
  document.documentElement.style.overflow = '';
  if (scrollY) {
    window.scrollTo(0, parseInt(scrollY || '0') * -1);
  }
};

const handleTouchStart = (event: TouchEvent) => {
  touchStartX.value = event.touches[0].clientX;
  touchStartY.value = event.touches[0].clientY;
};

const handleTouchMove = (event: TouchEvent) => {
  touchEndX.value = event.touches[0].clientX;
  touchEndY.value = event.touches[0].clientY;

  // ページモード時はスクロールを防止
  if (viewMode.value === ImageShowMode.Page && !isDraggingProgress.value) {
    event.preventDefault();
  }
};

const handleTouchEnd = () => {
  if (isDraggingProgress.value) {
    isDraggingProgress.value = false;
    return;
  }

  const deltaX = touchEndX.value - touchStartX.value;
  const deltaY = touchEndY.value - touchStartY.value;

  // 横方向のスワイプが縦方向より大きい場合のみ処理
  if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > minSwipeDistance) {
    if (deltaX > 0) {
      // 右スワイプ：前のページ
      prevPage();
    } else {
      // 左スワイプ：次のページ
      nextPage();
    }
    // スワイプ時はヘッダーを非表示
    showHeader.value = false;
  }

  // リセット
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

  // マウス移動とマウスアップのイベントリスナーを追加
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
  const newPage = Math.floor(percentage * imagePositions.value.length);
  currentPage.value = Math.max(0, Math.min(newPage, imagePositions.value.length - 1));
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
  const newPage = Math.floor(percentage * imagePositions.value.length);
  currentPage.value = Math.max(0, Math.min(newPage, imagePositions.value.length - 1));

  event.stopPropagation();
  event.preventDefault();
};

const handleProgressTouchEnd = (event: TouchEvent) => {
  isDraggingProgress.value = false;
  event.stopPropagation();
};

const handleScroll = () => {
  if (viewMode.value !== ImageShowMode.Scroll) return;

  // スクロールされたらヘッダーを非表示
  showHeader.value = false;
  lastScrollY.value = window.scrollY;
};

const handleScreenClick = (event: MouseEvent) => {
  if (viewMode.value !== ImageShowMode.Scroll) return;

  // ヘッダーやメニュー内のクリックは無視
  const target = event.target as HTMLElement;
  if (target.closest('.header') || target.closest('.menu-overlay')) {
    return;
  }

  // 画面中央付近をクリックした場合のみヘッダー表示を切り替え
  const screenHeight = window.innerHeight;
  const clickY = event.clientY;
  const centerThreshold = screenHeight * 0.3; // 上下30%の中央60%エリア

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

  // 中央エリアをクリックした場合：ヘッダー表示切り替え
  if (clickX > leftThird && clickX < rightThird) {
    showHeader.value = !showHeader.value;
  }
  // 左側クリック：前のページ
  else if (clickX < leftThird) {
    prevPage();
  }
  // 右側クリック：次のページ
  else if (clickX > rightThird) {
    nextPage();
  }
};

const prevPage = () => {
  if (currentPage.value > 0) {
    currentPage.value--;
    showHeader.value = false;

    // 次のページをプリロード
    if (currentPage.value > 0) {
      fetchImage(imagePositions.value[currentPage.value - 1]);
    }
  }
};

const nextPage = () => {
  if (currentPage.value < imagePositions.value.length - 1) {
    currentPage.value++;
    showHeader.value = false;

    // 次のページをプリロード
    if (currentPage.value < imagePositions.value.length - 1) {
      fetchImage(imagePositions.value[currentPage.value + 1]);
    }
  }
};

const onPageSelect = () => {
  showHeader.value = false;
};

const toggleMenu = () => {
  showMenu.value = !showMenu.value;
};

const changeMode = async (mode: ImageShowMode) => {
  viewMode.value = mode;
  showMenu.value = false;
  showHeader.value = false;
  isLoading.value = true;

  // モードに応じてbodyのスクロールを制御
  if (mode === ImageShowMode.Page) {
    lockScroll();
  } else {
    unlockScroll();
    lastScrollY.value = window.scrollY;
  }

  setImageShowMode(mode);

  // モード切替時に画像を再プリロード
  await preloadImages();
};

const router = useRouter();
const itemName = ref('');

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
  -webkit-transform: translateZ(0);
  transform: translateZ(0);
}

.header.hidden {
  transform: translateY(-100%);
  -webkit-transform: translateY(-100%);
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
  -webkit-transform: translateZ(0);
  transform: translateZ(0);
}

.progress-footer.hidden {
  transform: translateY(100%);
  -webkit-transform: translateY(100%);
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
  -webkit-user-select: none;
}

.progress-fill {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  -webkit-transform: translateY(-50%);
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
  -webkit-transform: translate(-50%, -50%);
  width: 16px;
  height: 16px;
  background: var(--accent);
  border-radius: 50%;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
  transition: left 0.1s ease;
  pointer-events: none;
  cursor: grab;
  cursor: -webkit-grab;
}

.progress-thumb:active {
  cursor: grabbing;
  cursor: -webkit-grabbing;
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
  -webkit-tap-highlight-color: transparent;
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
  -webkit-tap-highlight-color: transparent;
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
  -webkit-appearance: none;
  appearance: none;
  -webkit-tap-highlight-color: transparent;
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
  -webkit-tap-highlight-color: transparent;
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

.image-viewer {
  min-height: 100vh;
  min-height: 100dvh;
}

.scroll-mode {
  max-width: 900px;
  margin: 0 auto;
  padding: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
}

.image-wrapper {
  position: relative;
  width: 100%;
  min-height: 200px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.image-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  min-height: 200px;
  background: transparent;
}

.page-image {
  max-width: 100%;
  max-height: 100vh;
  max-height: 100dvh;
  width: auto;
  height: auto;
  margin-bottom: 1rem;
  border-radius: 8px;
  object-fit: contain;
}

.page-mode {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  width: 100dvw;
  height: 100vh;
  height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  overflow: hidden;
  touch-action: pan-y pinch-zoom;
  overscroll-behavior: contain;
  -webkit-overflow-scrolling: touch;
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
  -webkit-user-select: none;
  object-fit: contain;
  -webkit-touch-callout: none;
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

/* Safari特有のスタイル調整 */
@supports (-webkit-touch-callout: none) {
  .page-mode {
    height: -webkit-fill-available;
  }

  .image-viewer {
    min-height: -webkit-fill-available;
  }
}
</style>
