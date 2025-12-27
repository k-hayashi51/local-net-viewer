<template>
  <div class="viewer-page">
    <header class="header" :class="{ 'hidden': !showHeader }">
      <button @click="goBack" class="back-button">
        ← 戻る
      </button>
      <h2 class="title">{{ itemName }}</h2>
      <div v-if="viewMode === ImagePageMode.Page" class="page-selector-header">
        <select v-model.number="currentPage" @change="onPageSelect" class="page-select-header">
          <option v-for="(_, index) in imagePositions" :key="index" :value="index">
            {{ index + 1 }}
          </option>
        </select>
        <span class="page-total">/ {{ imagePositions.length }}</span>
      </div>
      <button @click="toggleMenu" class="hamburger-button">
        <span class="hamburger-icon">
          <span></span>
          <span></span>
          <span></span>
        </span>
      </button>
    </header>

    <!-- Progress Bar Footer -->
    <div v-if="viewMode === ImagePageMode.Page" class="progress-footer" :class="{ 'hidden': !showHeader }">
      <div 
        class="progress-bar" 
        @mousedown="handleProgressMouseDown"
        @touchstart="handleProgressTouchStart"
        @touchmove="handleProgressTouchMove"
        @touchend="handleProgressTouchEnd"
      >
        <div class="progress-fill" :style="{ width: progressPercentage + '%' }"></div>
        <div class="progress-thumb" :style="{ left: progressPercentage + '%' }"></div>
      </div>
      <div class="progress-text">{{ currentPage + 1 }} / {{ imagePositions.length }}</div>
    </div>

    <!-- Hamburger Menu -->
    <div v-if="showMenu" class="menu-overlay" @click="toggleMenu">
      <div class="menu-content" @click.stop>
        <h3 class="menu-title">表示モード</h3>
        <button 
          @click="changeMode(ImagePageMode.Scroll)" 
          :class="{ 'active': viewMode === ImagePageMode.Scroll }"
          class="menu-option"
        >
          📜 縦スクロール
        </button>
        <button 
          @click="changeMode(ImagePageMode.Page)" 
          :class="{ 'active': viewMode === ImagePageMode.Page }"
          class="menu-option"
        >
          📖 ページ送り
        </button>
      </div>
    </div>

    <!-- Image -->
    <div class="image-viewer">
      <div v-if="viewMode === ImagePageMode.Scroll" class="scroll-mode">
        <img
          v-for="(imagePosition, index) in imagePositions"
          :key="index"
          :src="`/api/files/${imagePosition}`"
          :alt="`Page ${index + 1}`"
          loading="lazy"
          class="page-image"
        />
      </div>

      <div 
        v-else 
        class="page-mode" 
        :class="{ 'has-header': showHeader }"
        @click="handlePageClick"
        @touchstart="handleTouchStart"
        @touchmove="handleTouchMove"
        @touchend="handleTouchEnd"
      >
        <div v-if="imagePositions.length > 0" class="page-container">
          <img 
            :src="`/api/files/${imagePositions[currentPage]}`" 
            :alt="`Page ${currentPage + 1}`" 
            class="current-page" 
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { FileInfoViewModel, FileType, ImagePageMode } from '../types';
import { getImagePageMode, setImagePageMode, setPosition } from '../services/LocalStorageService';

const props = defineProps<{
  position: string
}>()

const viewMode = ref<ImagePageMode>(ImagePageMode.Scroll)
const currentPage = ref(0)
const imagePositions = ref<string[]>([])
const showHeader = ref(true)
const showMenu = ref(false)
const lastScrollY = ref(0)
const scrollThreshold = 50

// タッチイベント用の変数
const touchStartX = ref(0)
const touchStartY = ref(0)
const touchEndX = ref(0)
const touchEndY = ref(0)
const minSwipeDistance = 50
const isDraggingProgress = ref(false)

const progressPercentage = computed(() => {
  if (imagePositions.value.length === 0) return 0
  return ((currentPage.value + 1) / imagePositions.value.length) * 100
})

onMounted(async () => {
  viewMode.value = getImagePageMode();
  
  const parentPosition = props.position.split('-').slice(0, -1).join("-")
  const response = await fetch(`/api/files/${parentPosition}/child`)
  const fileInfoList = (await response.json()) as FileInfoViewModel[]
  imagePositions.value = fileInfoList.filter(x => x.fileType === FileType.Image).map(x => x.position)
  
  // 初期表示位置の設定
  const initialIndex = imagePositions.value.findIndex(pos => pos === props.position)
  if (initialIndex !== -1) {
    currentPage.value = initialIndex
    
    // スクロールモードの場合、該当画像までスクロール
    if (viewMode.value === ImagePageMode.Scroll) {
      setTimeout(() => {
        const images = document.querySelectorAll('.page-image')
        if (images[initialIndex]) {
          images[initialIndex].scrollIntoView({ behavior: 'smooth', block: 'start' })
        }
      }, 100)
    }
  }
  
  window.addEventListener('scroll', handleScroll)
  window.addEventListener('click', handleScreenClick)
  
  setPosition(parentPosition);
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  window.removeEventListener('click', handleScreenClick)
  document.removeEventListener('mousemove', handleProgressMouseMove)
  document.removeEventListener('mouseup', handleProgressMouseUp)
})

const handleTouchStart = (event: TouchEvent) => {
  touchStartX.value = event.touches[0].clientX
  touchStartY.value = event.touches[0].clientY
}

const handleTouchMove = (event: TouchEvent) => {
  touchEndX.value = event.touches[0].clientX
  touchEndY.value = event.touches[0].clientY
}

const handleTouchEnd = () => {
  if (isDraggingProgress.value) {
    isDraggingProgress.value = false
    return
  }
  
  const deltaX = touchEndX.value - touchStartX.value
  const deltaY = touchEndY.value - touchStartY.value
  
  // 横方向のスワイプが縦方向より大きい場合のみ処理
  if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > minSwipeDistance) {
    if (deltaX > 0) {
      // 右スワイプ：前のページ
      prevPage()
    } else {
      // 左スワイプ：次のページ
      nextPage()
    }
  }
  
  // リセット
  touchStartX.value = 0
  touchStartY.value = 0
  touchEndX.value = 0
  touchEndY.value = 0
}

const handleProgressMouseDown = (event: MouseEvent) => {
  isDraggingProgress.value = true
  updateProgressPosition(event.clientX, event.currentTarget as HTMLElement)
  event.stopPropagation()
  event.preventDefault()
  
  // マウス移動とマウスアップのイベントリスナーを追加
  document.addEventListener('mousemove', handleProgressMouseMove)
  document.addEventListener('mouseup', handleProgressMouseUp)
}

const handleProgressMouseMove = (event: MouseEvent) => {
  if (!isDraggingProgress.value) return
  
  const progressBar = document.querySelector('.progress-bar') as HTMLElement
  if (progressBar) {
    updateProgressPosition(event.clientX, progressBar)
  }
  event.preventDefault()
}

const handleProgressMouseUp = () => {
  isDraggingProgress.value = false
  document.removeEventListener('mousemove', handleProgressMouseMove)
  document.removeEventListener('mouseup', handleProgressMouseUp)
}

const updateProgressPosition = (clientX: number, element: HTMLElement) => {
  const rect = element.getBoundingClientRect()
  const x = clientX - rect.left
  const percentage = Math.max(0, Math.min(1, x / rect.width))
  const newPage = Math.floor(percentage * imagePositions.value.length)
  currentPage.value = Math.max(0, Math.min(newPage, imagePositions.value.length - 1))
}

const handleProgressTouchStart = (event: TouchEvent) => {
  isDraggingProgress.value = true
  event.stopPropagation()
}

const handleProgressTouchMove = (event: TouchEvent) => {
  if (!isDraggingProgress.value) return
  
  const target = event.currentTarget as HTMLElement
  const rect = target.getBoundingClientRect()
  const touchX = event.touches[0].clientX - rect.left
  const percentage = Math.max(0, Math.min(1, touchX / rect.width))
  const newPage = Math.floor(percentage * imagePositions.value.length)
  currentPage.value = Math.max(0, Math.min(newPage, imagePositions.value.length - 1))
  
  event.stopPropagation()
  event.preventDefault()
}

const handleProgressTouchEnd = (event: TouchEvent) => {
  isDraggingProgress.value = false
  event.stopPropagation()
}

const handleScroll = () => {
  if (viewMode.value !== ImagePageMode.Scroll) return
  
  const currentScrollY = window.scrollY
  const scrollDiff = currentScrollY - lastScrollY.value
  
  // 閾値を超えたスクロールのみ反応
  if (Math.abs(scrollDiff) > scrollThreshold) {
    if (scrollDiff > 0) {
      // 下スクロール
      showHeader.value = false
    } else {
      // 上スクロール
      showHeader.value = true
    }
    lastScrollY.value = currentScrollY
  }
}

const handleScreenClick = (event: MouseEvent) => {
  if (viewMode.value !== ImagePageMode.Scroll) return
  
  // ヘッダーやメニュー内のクリックは無視
  const target = event.target as HTMLElement
  if (target.closest('.header') || target.closest('.menu-overlay')) {
    return
  }
  
  // 画面タッチでヘッダー表示/非表示切り替え
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
    // 左側クリック：前のページ
    prevPage()
  } else if (clickX > rightThird) {
    // 右側クリック：次のページ
    nextPage()
  } else {
    // 中央クリック：ヘッダー表示切り替え
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
  if (currentPage.value < imagePositions.value.length - 1) {
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

const changeMode = (mode: ImagePageMode) => {
  viewMode.value = mode
  showMenu.value = false
  
  if (mode === ImagePageMode.Scroll) {
    showHeader.value = true
    lastScrollY.value = window.scrollY
  } else {
    // ページモードに切り替えた時はヘッダー非表示
    showHeader.value = false
  }

  setImagePageMode(mode);
}

const router = useRouter()
const itemName = ref('')

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

.image-viewer {
  min-height: 100vh;
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

.page-image {
  max-width: 100%;
  max-height: 100vh;
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
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  overflow: hidden;
  touch-action: pan-y pinch-zoom;
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