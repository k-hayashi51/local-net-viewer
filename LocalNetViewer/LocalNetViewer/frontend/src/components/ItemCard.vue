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
      <div class="type-badge">
        {{ typeBadge }}
      </div>
    </div>
    <div class="content">
      <h3 class="title">{{ item.name }}</h3>
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
  flex-direction: column;
  background: var(--bg-card);
  border-radius: 12px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.3s ease;
  border: 2px solid transparent;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.item-card:hover {
  border-color: var(--accent);
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
}

.thumbnail {
  position: relative;
  width: 100%;
  aspect-ratio: 1;
  background: var(--bg-secondary);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.item-card:hover .thumbnail img {
  transform: scale(1.05);
}

.icon-placeholder {
  font-size: 4rem;
  opacity: 0.7;
}

.type-badge {
  position: absolute;
  top: 0.75rem;
  right: 0.75rem;
  background: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(8px);
  padding: 0.375rem 0.75rem;
  border-radius: 20px;
  font-size: 0.75rem;
  color: white;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

.content {
  padding: 1rem;
  background: var(--bg-card);
}

.title {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  line-clamp: 2;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.4;
  min-height: 2.8em;
  margin: 0;
}

@media (max-width: 768px) {
  .icon-placeholder {
    font-size: 3rem;
  }

  .type-badge {
    top: 0.5rem;
    right: 0.5rem;
    padding: 0.25rem 0.625rem;
    font-size: 0.7rem;
  }

  .content {
    padding: 0.75rem;
  }

  .title {
    font-size: 0.875rem;
  }
}

@media (max-width: 480px) {
  .icon-placeholder {
    font-size: 2.5rem;
  }

  .type-badge {
    font-size: 0.65rem;
    padding: 0.25rem 0.5rem;
  }
}
</style>