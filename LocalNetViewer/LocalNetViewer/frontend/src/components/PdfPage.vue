<template>
  <div class="viewer-page">
    <header class="header" :class="{ 'hidden': !showHeader }">
      <button @click="goBack" class="back-button">
        ← 戻る
      </button>
      <h2 class="title">{{ itemName }}</h2>
      <div v-if="viewMode === 'page'" class="page-selector-header">
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

    <!-- Hamburger Menu -->
    <div v-if="showMenu" class="menu-overlay" @click="toggleMenu">
      <div class="menu-content" @click.stop>
        <h3 class="menu-title">表示モード</h3>
        <button 
          @click="changeMode('scroll')" 
          :class="{ 'active': viewMode === 'scroll' }"
          class="menu-option"
        >
          📜 縦スクロール
        </button>
        <button 
          @click="changeMode('page')" 
          :class="{ 'active': viewMode === 'page' }"
          class="menu-option"
        >
          📖 ページ送り
        </button>
      </div>
    </div>

    <!-- PDF Viewer -->
    <div v-if="isLoading" class="loading">
      <div class="spinner"></div>
    </div>

    <div v-else class="pdf-viewer">
      <div v-if="viewMode === 'scroll'" class="scroll-mode">
        <canvas
          v-for="pageNum in totalPages"
          :key="pageNum"
          :ref="(el) => setCanvasRef(el, pageNum - 1)"
          class="pdf-page"
        ></canvas>
      </div>

      <div v-else class="page-mode" @click="handlePageClick">
        <div class="page-container">
          <canvas
            ref="currentPageCanvas"
            class="current-page"
          ></canvas>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { PDFDocumentProxy } from 'pdfjs-dist'

// PDF.jsを動的インポートするための型定義
let pdfjsLib: typeof import('pdfjs-dist') | null = null

const props = defineProps<{
  position: string
}>()

const viewMode = ref<'scroll' | 'page'>('scroll')
const currentPage = ref(0)
const totalPages = ref(0)
const showHeader = ref(true)
const showMenu = ref(false)
const lastScrollY = ref(0)
const scrollThreshold = 50
const isLoading = ref(true)
const itemName = ref('')

let pdfDoc: PDFDocumentProxy | null = null
const canvasRefs = ref<HTMLCanvasElement[]>([])
const currentPageCanvas = ref<Element | null>(null)

const setCanvasRef = (el: unknown, index: number) => {
  if (el && el instanceof HTMLCanvasElement) {
    canvasRefs.value[index] = el
  }
}

onMounted(async () => {
  await loadPDF()
  window.addEventListener('scroll', handleScroll)
  window.addEventListener('click', handleScreenClick)
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  window.removeEventListener('click', handleScreenClick)
})

const loadPDF = async () => {
  try {
    // PDF.jsを動的にインポート（コード分割）
    if (!pdfjsLib) {
      pdfjsLib = await import('pdfjs-dist')
      
      // Worker設定
      pdfjsLib.GlobalWorkerOptions.workerSrc = new URL(
        'pdfjs-dist/build/pdf.worker.min.mjs',
        import.meta.url
      ).toString()
    }
    
    // PDFファイルの読み込み
    const response = await fetch(`/api/files/${props.position}`)
    const arrayBuffer = await response.arrayBuffer()
    
    const loadingTask = pdfjsLib.getDocument({ data: arrayBuffer })
    pdfDoc = await loadingTask.promise
    totalPages.value = pdfDoc.numPages
    
    isLoading.value = false
    
    // 初期表示
    if (viewMode.value === 'scroll') {
      setTimeout(() => renderAllPages(), 100)
    } else {
      setTimeout(() => renderPage(currentPage.value), 100)
    }
  } catch (error) {
    console.error('PDF読み込みエラー:', error)
    isLoading.value = false
  }
}

const renderPage = async (pageNum: number) => {
  if (!pdfDoc) return
  
  try {
    const page = await pdfDoc.getPage(pageNum + 1)
    let canvas: HTMLCanvasElement | null = null
    
    if (viewMode.value === 'page') {
      const el = currentPageCanvas.value
      canvas = el instanceof HTMLCanvasElement ? el : null
    } else {
      canvas = canvasRefs.value[pageNum] || null
    }
    
    if (!canvas) return
    
    const context = canvas.getContext('2d')
    if (!context) return
    
    // ビューポートの計算
    const viewport = page.getViewport({ scale: 1.0 })
    
    // 画面サイズに合わせたスケール計算
    const maxWidth = window.innerWidth
    const maxHeight = window.innerHeight
    const scaleX = maxWidth / viewport.width
    const scaleY = maxHeight / viewport.height
    const scale = Math.min(scaleX, scaleY, 2.0) // 最大2倍まで
    
    const scaledViewport = page.getViewport({ scale })
    
    canvas.width = scaledViewport.width
    canvas.height = scaledViewport.height
    
    const renderContext = {
      canvasContext: context,
      viewport: scaledViewport,
      canvas: canvas
    }
    
    await page.render(renderContext).promise
  } catch (error) {
    console.error(`ページ${pageNum + 1}のレンダリングエラー:`, error)
  }
}

const renderAllPages = async () => {
  for (let i = 0; i < totalPages.value; i++) {
    await renderPage(i)
  }
}

watch(currentPage, (newPage) => {
  if (viewMode.value === 'page' && !isLoading.value) {
    setTimeout(() => renderPage(newPage), 50)
  }
})

watch(viewMode, (newMode) => {
  if (newMode === 'scroll' && !isLoading.value) {
    setTimeout(() => renderAllPages(), 100)
  } else if (newMode === 'page' && !isLoading.value) {
    setTimeout(() => renderPage(currentPage.value), 100)
  }
})

const handleScroll = () => {
  if (viewMode.value !== 'scroll') return
  
  const currentScrollY = window.scrollY
  const scrollDiff = currentScrollY - lastScrollY.value
  
  if (Math.abs(scrollDiff) > scrollThreshold) {
    if (scrollDiff > 0) {
      showHeader.value = false
    } else {
      showHeader.value = true
    }
    lastScrollY.value = currentScrollY
  }
}

const handleScreenClick = (event: MouseEvent) => {
  if (viewMode.value !== 'scroll') return
  
  const target = event.target as HTMLElement
  if (target.closest('.header') || target.closest('.menu-overlay')) {
    return
  }
  
  showHeader.value = !showHeader.value
}

const handlePageClick = (event: MouseEvent) => {
  const target = event.currentTarget as HTMLElement
  const rect = target.getBoundingClientRect()
  const clickX = event.clientX - rect.left
  const width = rect.width
  
  const leftThird = width / 3
  const rightThird = width * 2 / 3
  
  if (clickX < leftThird) {
    prevPage()
  } else if (clickX > rightThird) {
    nextPage()
  } else {
    showHeader.value = !showHeader.value
  }
}

const prevPage = () => {
  if (currentPage.value > 0) {
    currentPage.value--
    showHeader.value = false
  }
}

const nextPage = () => {
  if (currentPage.value < totalPages.value - 1) {
    currentPage.value++
    showHeader.value = false
  }
}

const onPageSelect = () => {
  showHeader.value = false
}

const toggleMenu = () => {
  showMenu.value = !showMenu.value
}

const changeMode = (mode: 'scroll' | 'page') => {
  viewMode.value = mode
  showMenu.value = false
  
  if (mode === 'scroll') {
    showHeader.value = true
    lastScrollY.value = window.scrollY
  } else {
    showHeader.value = false
  }
}

const router = useRouter()

const goBack = () => {
  const parentPosition = props.position.split('-').slice(0, -1).join("-")
  router.push(`/${parentPosition}`)
}
</script>

<style scoped>
.viewer-page {
  min-height: 100vh;
  background: var(--bg-primary);
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

.loading {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--bg-card);
  border-top-color: var(--accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
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
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  overflow: hidden;
}

.page-container {
  position: relative;
  width: 100vw;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.current-page {
  max-width: 100vw;
  max-height: 100vh;
  width: 100vw;
  height: 100vh;
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