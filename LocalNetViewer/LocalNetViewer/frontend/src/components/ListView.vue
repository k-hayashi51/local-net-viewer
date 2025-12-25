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

    <div class="list-container" v-else>
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
const breadcrumbs = ref<Breadcrumb[]>([{name: 'Top', position: ''}])

onMounted(async () => {
  url.value = await (await fetch(`/api/network/url`)).text()
  await updateFilesAsync()
})

watch(() => props.position, () => updateFilesAsync())

const updateFilesAsync = async () => {
  let position = props.position
  if (!props.position) {
    const positionRes = await fetch(`/api/settings/position`)
    position = await positionRes.text()
  }
  
  if (position) {
    const response = await fetch(`/api/files/${position}/child`)
    files.value = await response.json()
    
    // パンくずリストの取得
    await updateBreadcrumbs(position)

    await fetch('/api/settings/position', {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(position)
    });
    return
  }
  
  const response = await fetch(`/api/files`)
  files.value = await response.json()
  breadcrumbs.value = [{name: 'Top', position: ''}]
}

const updateBreadcrumbs = async (position: string) => {
  try {
    // position からフルパスを取得
    const response = await fetch(`/api/files/${position}/path`);
    const fullPath = await response.text();

    // ディレクトリ名配列
    const dirNames = fullPath.split(/[\\/]/);

    // パンくず用 position 分解
    const positionIds = position.split('-');

    // パンくずリスト生成
    breadcrumbs.value = [{name: 'Top', position: ''}, ...positionIds
      .map((_, index): Breadcrumb => ({
        name: dirNames[index],
        position: positionIds.slice(0, index + 1).join('-'),
      }))];

  } catch (error) {
    console.error('パンくずリストの取得エラー:', error)
    breadcrumbs.value = [{name: 'Top', position: ''}]
  }
}

const navigateToAsync = async (position: string) => {
  if (position) {
    router.push(`/${position}`)
  } else {
    await fetch('/api/settings/position', {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify('')
    });
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

.list-container {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.empty {
  text-align: center;
  padding: 4rem 2rem;
  color: var(--text-tertiary);
  font-size: 1.125rem;
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