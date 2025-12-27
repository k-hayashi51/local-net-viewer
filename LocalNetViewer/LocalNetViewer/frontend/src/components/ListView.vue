<template>
  <div class="list-view">
    <Disclosure>
      <DisclosureButton class="header">
        <h1>📚 Document Viewer</h1>
      </DisclosureButton>

      <DisclosurePanel>
        <div class="qr-wrapper">
          <div class="qr-card">
            <qrcode-vue :value="url" :size="200" />
            <p class="qr-url">{{ url }}</p>
          </div>
        </div>
      </DisclosurePanel>
    </Disclosure>

    <!-- Breadcrumb -->
    <nav class="breadcrumb">
      <button
        @click="navigateToAsync('')"
        class="breadcrumb-item"
        :class="{ 'active': breadcrumbs.length === 0 }">Top</button>
      <button
        v-for="(crumb, index) in breadcrumbs"
        :key="crumb.position"
        @click="navigateToAsync(crumb.position)"
        class="breadcrumb-item"
        :class="{ 'active': index === breadcrumbs.length - 1 }"
      >
        {{ crumb.name }}
        <span v-if="index < breadcrumbs.length - 1" class="separator">/</span>
      </button>
    </nav>

    <div class="loading" v-if="loading">
      <div class="spinner"></div>
    </div>

    <div class="grid-container" v-else>
      <ItemCard
        v-for="item in files"
        :key="item.position"
        :item="item"
        @click="handleItemClick(item)"
      />
    </div>

    <div class="empty" v-if="!loading && files.length === 0">
      <p>📂 フォルダを選択してください</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import ItemCard from './ItemCard.vue'
import { Disclosure, DisclosureButton, DisclosurePanel } from '@headlessui/vue'
import QrcodeVue from "qrcode.vue";
import { FileType, type FileInfoViewModel } from '../types'
import { getPosition, setPosition } from '../services/LocalStorageService';

interface Breadcrumb {
  name: string
  position: string
}

const props = defineProps<{
  position?: string
}>()

const router = useRouter()
const files = ref<FileInfoViewModel[]>([])
const loading = ref(false)
const url = ref('')
const breadcrumbs = ref<Breadcrumb[]>([])

onMounted(async () => {
  // 接続用URLを取得する。
  url.value = await (await fetch(`/api/network/url`)).text()
  // 表示ファイルを更新する。
  await updateFilesAsync()
})

watch(() => props.position, async () => await updateFilesAsync())

/**
 * 表示ファイルを更新する。
 */
const updateFilesAsync = async () => {
  // URLに指定のディレクトリ位置が存在する場合、ファイルリストを取得する。
  if (props.position) {
    let response = new Response();
    try {
      // 指定したディレクトリ位置に表示されている子ディレクトリ情報を取得する。
      response = await fetch(`/api/files/${props.position}/child`)
    } catch {
      // 子ディレクトリ情報の取得に失敗した場合、トップに戻す。
      navigateToAsync('');
      return;
    }
    files.value = await response.json()
    await updateBreadcrumbs(props.position) // パンくずリストを更新する。
    setPosition(props.position); // ローカルセッションに使用したディレクトリ位置を記憶する。
    return
  }

  // URLに指定のディレクトリ位置が存在しない場合、ローカルセッションに登録されているディレクトリ位置を表示させる。
  const position = getPosition();
  if (position) {
    navigateToAsync(position);
    return;
  }
  
  // URLにもローカルセッションにもディレクトリ位置がない場合、トップに戻す。
  const response = await fetch(`/api/files`)
  files.value = await response.json()
  breadcrumbs.value = []
}

const updateBreadcrumbs = async (position: string) => {
  try {
    const response = await fetch(`/api/files/${position}/path`);
    const fullPath = await response.text();

    const dirNames = fullPath.split(/[\\/]/);
    const positionIds = position.split('-');

    breadcrumbs.value = positionIds
      .map((_, index): Breadcrumb => ({
        name: dirNames[index],
        position: positionIds.slice(0, index + 1).join('-'),
      }));

  } catch (error) {
    console.error('パンくずリストの取得エラー:', error)
    breadcrumbs.value = []
  }
}

const navigateToAsync = async (position: string) => {
  if (position) {
    router.push(`/${position}`)
  } else {
    setPosition('');
    router.push('/')
  }
}

const handleItemClick = (item: FileInfoViewModel) => {
  if (item.isDirectory) {
    router.push(`/${item.position}`)
    return
  }
  switch (item.fileType) {
    case FileType.Image:
      router.push(`/image/${item.position}`)
      return
    case FileType.Pdf:
      router.push(`/pdf/${item.position}`)
      return
    case FileType.Video:
      router.push(`/video/${item.position}`)
      return
  }
}
</script>

<style scoped>
.list-view {
  min-height: 100vh;
  padding: 2rem;
}

.header {
  max-width: 1400px;
  margin: 0 auto 3rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1.5rem;
}

h1 {
  font-size: 2rem;
  font-weight: 700;
}

.breadcrumb {
  max-width: 1400px;
  margin: 0 auto 2rem;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  padding: 1rem;
  background: var(--bg-secondary);
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.breadcrumb-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: var(--bg-card);
  border: none;
  border-radius: 8px;
  font-weight: 500;
  color: var(--text-primary);
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 200px;
}

.breadcrumb-item:hover:not(.active) {
  background: var(--accent);
  transform: translateY(-1px);
}

.breadcrumb-item.active {
  background: var(--accent);
  cursor: default;
  font-weight: 600;
}

.separator {
  color: var(--text-tertiary);
  font-weight: 400;
  margin-left: 0.25rem;
}

.loading {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
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

.grid-container {
  max-width: 1400px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 1.5rem;
}

.empty {
  text-align: center;
  padding: 4rem 2rem;
  color: var(--text-tertiary);
  font-size: 1.125rem;
}

@media (max-width: 1024px) {
  .grid-container {
    grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    gap: 1.25rem;
  }
}

@media (max-width: 768px) {
  .list-view {
    padding: 1rem;
  }

  .header {
    margin-bottom: 2rem;
  }

  h1 {
    font-size: 1.5rem;
  }

  .breadcrumb {
    margin-bottom: 1.5rem;
    padding: 0.75rem;
  }

  .breadcrumb-item {
    padding: 0.375rem 0.75rem;
    font-size: 0.875rem;
    max-width: 120px;
  }

  .grid-container {
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
    gap: 1rem;
  }
}

.qr-wrapper {
  display: flex;
  justify-content: center;
  margin-top: 2rem;
}

.qr-card {
  background: #ffffff;
  padding: 1.5rem;
  border-radius: 1.5rem;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.12);
  text-align: center;
  width: fit-content;
}

.qr-url {
  margin-top: 1rem;
  font-size: 0.9rem;
  color: #333;
  word-break: break-all;
}
</style>