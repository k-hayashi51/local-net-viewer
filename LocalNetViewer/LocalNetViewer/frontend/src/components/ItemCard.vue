<template>
  <div class="item-card" @click="emit('click')">
    <div class="thumbnail">
      <img
        v-if="props.item.fileType === FileType.Image"
        :src="`/api/files/${item.position}/thumbnail`"
        :alt="item.name"
        loading="lazy" />
      <img
        v-else-if="item.childImagePositions.length !== 0"
        :src="`/api/files/${item.childImagePositions[0]}/thumbnail`"
        :alt="item.name"
        loading="lazy" />
      <div v-else class="icon-placeholder">
        {{ typeIcon }}
      </div>
    </div>
    <div class="content">
      <h3 class="title">{{ item.name }}</h3>
      <div class="type-badge">
        {{ typeBadge }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { FileType, type FileInfoViewModel } from '../types'

const props = defineProps<{
  item: FileInfoViewModel
}>()

const emit = defineEmits<{
  click: []
}>()

const typeIcon = computed(() => {
  if (props.item.isDirectory) {
    return '📁' 
  }
  switch (props.item.fileType) {
    case FileType.Image: return '🖼️'
    case FileType.Pdf: return '📄'
    case FileType.Video: return '🎬'
    default: return '📄'
  }
})

const typeBadge = computed(() => {
  if (props.item.isDirectory) {
    return 'フォルダ' 
  }
  switch (props.item.fileType) {
    case FileType.Image: return '画像'
    case FileType.Pdf: return 'PDF'
    case FileType.Video: return '動画'
    default: return 'ファイル'
  }
})
</script>

<style scoped>
.item-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  background: var(--bg-card);
  border-radius: 8px;
  padding: 0.75rem;
  cursor: pointer;
  transition: all 0.2s;
  border: 1px solid transparent;
}

.item-card:hover {
  border-color: var(--accent);
  background: var(--bg-secondary);
  transform: translateX(4px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.thumbnail {
  flex-shrink: 0;
  width: 64px;
  height: 64px;
  border-radius: 6px;
  overflow: hidden;
  background: var(--bg-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
}

.thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.icon-placeholder {
  font-size: 2rem;
}

.content {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 1rem;
}

.title {
  flex: 1;
  font-size: 1rem;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--text-primary);
}

.type-badge {
  flex-shrink: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(8px);
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  font-size: 0.75rem;
  color: var(--text-secondary);
  font-weight: 500;
}

@media (max-width: 768px) {
  .item-card {
    padding: 0.625rem;
    gap: 0.75rem;
  }

  .thumbnail {
    width: 48px;
    height: 48px;
  }

  .icon-placeholder {
    font-size: 1.5rem;
  }

  .title {
    font-size: 0.875rem;
  }

  .type-badge {
    font-size: 0.7rem;
    padding: 0.2rem 0.5rem;
  }
}
</style>