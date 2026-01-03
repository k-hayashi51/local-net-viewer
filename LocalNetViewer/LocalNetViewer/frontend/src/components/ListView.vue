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
      <button @click="navigateToAsync('')" class="breadcrumb-item" :class="{ active: breadcrumbs.length === 0 }">Top</button>
      <button v-for="(crumb, index) in breadcrumbs" :key="crumb.position" @click="navigateToAsync(crumb.position)" class="breadcrumb-item" :class="{ active: index === breadcrumbs.length - 1 }">
        {{ crumb.name }}
        <span v-if="index < breadcrumbs.length - 1" class="separator">/</span>
      </button>
    </nav>

    <div class="loading" v-if="loading">
      <div class="spinner"></div>
    </div>

    <template v-else>
      <!-- Pagination Controls (Top) -->
      <div class="pagination pagination-top" v-if="totalPages > 1">
        <button @click="goToPage(1)" :disabled="currentPage === 1" class="page-button">≪</button>
        <button @click="goToPage(currentPage - 1)" :disabled="currentPage === 1" class="page-button">‹</button>

        <template v-for="page in displayPages" :key="page">
          <span v-if="page === '...'" class="page-ellipsis">...</span>
          <button v-else @click="goToPage(page as number)" class="page-button" :class="{ active: currentPage === page }">
            {{ page }}
          </button>
        </template>

        <button @click="goToPage(currentPage + 1)" :disabled="currentPage === totalPages" class="page-button">›</button>
        <button @click="goToPage(totalPages)" :disabled="currentPage === totalPages" class="page-button">≫</button>

        <select v-model.number="itemsPerPage" @change="changeItemsPerPage" class="items-per-page">
          <option :value="12">12件</option>
          <option :value="24">24件</option>
          <option :value="48">48件</option>
          <option :value="96">96件</option>
        </select>

        <span class="page-info"> {{ (currentPage - 1) * itemsPerPage + 1 }}-{{ Math.min(currentPage * itemsPerPage, files.length) }} / {{ files.length }}件 </span>
      </div>

      <div class="grid-container">
        <ItemCard v-for="item in paginatedFiles" :key="item.position" :item="item" @click="handleItemClick(item)" />
      </div>

      <!-- Pagination Controls (Bottom) -->
      <div class="pagination pagination-bottom" v-if="totalPages > 1">
        <button @click="goToPage(1)" :disabled="currentPage === 1" class="page-button">≪</button>
        <button @click="goToPage(currentPage - 1)" :disabled="currentPage === 1" class="page-button">‹</button>

        <template v-for="page in displayPages" :key="page">
          <span v-if="page === '...'" class="page-ellipsis">...</span>
          <button v-else @click="goToPage(page as number)" class="page-button" :class="{ active: currentPage === page }">
            {{ page }}
          </button>
        </template>

        <button @click="goToPage(currentPage + 1)" :disabled="currentPage === totalPages" class="page-button">›</button>
        <button @click="goToPage(totalPages)" :disabled="currentPage === totalPages" class="page-button">≫</button>

        <select v-model.number="itemsPerPage" @change="changeItemsPerPage" class="items-per-page">
          <option :value="12">12件</option>
          <option :value="24">24件</option>
          <option :value="48">48件</option>
          <option :value="96">96件</option>
        </select>

        <span class="page-info"> {{ (currentPage - 1) * itemsPerPage + 1 }}-{{ Math.min(currentPage * itemsPerPage, files.length) }} / {{ files.length }}件 </span>
      </div>
    </template>

    <div class="empty" v-if="!loading && files.length === 0">
      <p>📂 フォルダを選択してください</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch, computed } from 'vue';
import { useRouter } from 'vue-router';
import ItemCard from './ItemCard.vue';
import { Disclosure, DisclosureButton, DisclosurePanel } from '@headlessui/vue';
import QrcodeVue from 'qrcode.vue';
import { FileType, type FileInfoViewModel } from '../types';
import { getPosition, setPosition } from '../services/LocalStorageService';

interface Breadcrumb {
  name: string;
  position: string;
}

const props = defineProps<{
  position?: string;
  page?: string;
  perPage?: string;
}>();

const router = useRouter();
const files = ref<FileInfoViewModel[]>([]);
const loading = ref(false);
const url = ref('');
const breadcrumbs = ref<Breadcrumb[]>([]);

// URLパラメータからページング情報を取得（存在しない場合はデフォルト値）
const currentPage = computed(() => {
  if (!props.page) return 1;
  const page = parseInt(props.page);
  return page > 0 ? page : 1;
});

const itemsPerPage = ref(24);

// propsからperPageを初期化
if (props.perPage) {
  const perPage = parseInt(props.perPage);
  if (perPage > 0) {
    itemsPerPage.value = perPage;
  }
}

// ページネーション計算
const totalPages = computed(() => Math.ceil(files.value.length / itemsPerPage.value));

const paginatedFiles = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage.value;
  const end = start + itemsPerPage.value;
  return files.value.slice(start, end);
});

const displayPages = computed(() => {
  const pages: (number | string)[] = [];
  const total = totalPages.value;
  const current = currentPage.value;

  if (total <= 7) {
    for (let i = 1; i <= total; i++) {
      pages.push(i);
    }
  } else {
    if (current <= 3) {
      for (let i = 1; i <= 4; i++) pages.push(i);
      pages.push('...');
      pages.push(total);
    } else if (current >= total - 2) {
      pages.push(1);
      pages.push('...');
      for (let i = total - 3; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      pages.push('...');
      for (let i = current - 1; i <= current + 1; i++) pages.push(i);
      pages.push('...');
      pages.push(total);
    }
  }

  return pages;
});

const goToPage = (page: number) => {
  if (page >= 1 && page <= totalPages.value) {
    const query: Record<string, string> = {
      page: page.toString(),
      perPage: itemsPerPage.value.toString(),
    };

    if (props.position) {
      router.push({ path: `/${props.position}`, query });
    } else {
      router.push({ path: '/', query });
    }

    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
};

const changeItemsPerPage = () => {
  goToPage(1);
};

// propsの変更を監視してitemsPerPageを更新
watch(
  () => props.perPage,
  (newPerPage) => {
    if (newPerPage) {
      const perPage = parseInt(newPerPage);
      if (perPage > 0) {
        itemsPerPage.value = perPage;
      }
    }
  },
);

onMounted(async () => {
  url.value = await (await fetch(`/api/network/url`)).text();
  await updateFilesAsync();
});

watch(
  () => props.position,
  async () => {
    await updateFilesAsync();
  },
);

const updateFilesAsync = async () => {
  if (props.position) {
    let response = new Response();
    try {
      response = await fetch(`/api/files/${props.position}/child`);
    } catch {
      navigateToAsync('');
      return;
    }
    files.value = await response.json();
    await updateBreadcrumbs(props.position);
    setPosition(props.position);
    return;
  }

  const position = getPosition();
  if (position) {
    navigateToAsync(position);
    return;
  }

  const response = await fetch(`/api/files`);
  files.value = await response.json();
  breadcrumbs.value = [];
};

const updateBreadcrumbs = async (position: string) => {
  try {
    const response = await fetch(`/api/files/${position}/path`);
    const fullPath = await response.text();

    const dirNames = fullPath.split(/[\\/]/);
    const positionIds = position.split('-');

    breadcrumbs.value = positionIds.map(
      (_, index): Breadcrumb => ({
        name: dirNames[index],
        position: positionIds.slice(0, index + 1).join('-'),
      }),
    );
  } catch (error) {
    console.error('パンくずリストの取得エラー:', error);
    breadcrumbs.value = [];
  }
};

const navigateToAsync = async (position: string) => {
  if (position) {
    router.push(`/${position}`);
  } else {
    setPosition('');
    router.push('/');
  }
};

const handleItemClick = (item: FileInfoViewModel) => {
  if (item.isDirectory) {
    router.push(`/${item.position}`);
    return;
  }
  switch (item.fileType) {
    case FileType.Image:
      router.push(`/image/${item.position}`);
      return;
    case FileType.Pdf:
      router.push(`/pdf/${item.position}`);
      return;
    case FileType.Video:
      router.push(`/video/${item.position}`);
      return;
  }
};
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
  to {
    transform: rotate(360deg);
  }
}

.grid-container {
  max-width: 1400px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 1.5rem;
}

.pagination {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  padding: 1.5rem;
  background: var(--bg-secondary);
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.pagination-top {
  margin-bottom: 2rem;
}

.pagination-bottom {
  margin-top: 3rem;
}

.page-button {
  min-width: 40px;
  height: 40px;
  padding: 0 0.75rem;
  background: var(--bg-card);
  border: 2px solid transparent;
  border-radius: 8px;
  font-weight: 500;
  color: var(--text-primary);
  cursor: pointer;
  transition: all 0.2s;
}

.page-button:hover:not(:disabled):not(.active) {
  background: var(--accent);
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.page-button.active {
  background: var(--accent);
  border-color: var(--accent);
  font-weight: 700;
  cursor: default;
}

.page-button:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.page-ellipsis {
  padding: 0 0.5rem;
  color: var(--text-tertiary);
  font-weight: 500;
}

.items-per-page {
  margin-left: 1rem;
  padding: 0.5rem 1rem;
  background: var(--bg-card);
  border: 2px solid var(--bg-card);
  border-radius: 8px;
  font-weight: 500;
  color: var(--text-primary);
  cursor: pointer;
  transition: all 0.2s;
}

.items-per-page:hover {
  border-color: var(--accent);
}

.page-info {
  margin-left: 1rem;
  padding: 0.5rem 1rem;
  background: var(--bg-card);
  border-radius: 8px;
  font-weight: 500;
  color: var(--text-secondary);
  white-space: nowrap;
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

  .pagination {
    padding: 1rem;
    gap: 0.25rem;
  }

  .page-button {
    min-width: 36px;
    height: 36px;
    padding: 0 0.5rem;
    font-size: 0.875rem;
  }

  .items-per-page {
    font-size: 0.875rem;
    padding: 0.375rem 0.75rem;
  }

  .page-info {
    width: 100%;
    text-align: center;
    margin: 0.5rem 0 0 0;
    font-size: 0.875rem;
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
