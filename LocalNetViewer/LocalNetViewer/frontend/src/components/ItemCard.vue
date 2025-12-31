<template>
  <div class="item-card" @click="emit('click')">
    <div class="thumbnail" :class="{ 'is-grid': item.fileType !== FileType.Image && item.childImagePositions.length > 0 }">
      <!-- 画像 -->
      <img v-if="item.fileType === FileType.Image" :src="`/api/files/${item.position}/thumbnail`" :alt="item.name" loading="lazy" />

      <!-- PDF -->
      <object v-else-if="item.fileType === FileType.Pdf" :data="`/api/files/${item.position}/pdf/first#view=FitPage&toolbar=0&navpanes=0`" type="application/pdf" />

      <!-- フォルダ内の画像グリッド -->
      <template v-else-if="item.childImagePositions.length > 0">
        <img v-for="pos in item.childImagePositions.slice(0, 4)" :key="pos" :src="`/api/files/${pos}/thumbnail`" :alt="item.name" loading="lazy" />
      </template>

      <!-- その他 -->
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
import { computed } from 'vue';
import { FileType, type FileInfoViewModel } from '../types';

const props = defineProps<{
  item: FileInfoViewModel;
}>();

const emit = defineEmits<{
  click: [];
}>();

const typeIcon = computed(() => {
  if (props.item.isDirectory) return '📁';
  switch (props.item.fileType) {
    case FileType.Image:
      return '🖼️';
    case FileType.Pdf:
      return '📄';
    case FileType.Video:
      return '🎬';
    default:
      return '📄';
  }
});

const typeBadge = computed(() => {
  if (props.item.isDirectory) return 'フォルダ';
  switch (props.item.fileType) {
    case FileType.Image:
      return '画像';
    case FileType.Pdf:
      return 'PDF';
    case FileType.Video:
      return '動画';
    default:
      return 'ファイル';
  }
});
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
  align-items: center; /* 縦中央 */
  justify-content: center; /* 横中央 */
  overflow: hidden;
}

/* グリッド表示 */
.thumbnail.is-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-template-rows: repeat(2, 1fr);
}

.thumbnail img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  transition: transform 0.3s ease;
}

/* 単一画像のみホバー拡大 */
.item-card:hover .thumbnail:not(.is-grid) img {
  transform: scale(1.05);
}

.item-card:hover .thumbnail.is-grid img {
  transform: none;
}

/* プレースホルダ */
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
  color: #fff;
  font-weight: 600;
  letter-spacing: 0.5px;
  z-index: 1;
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
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.4;
  min-height: 2.8em;
  margin: 0;
}

/* レスポンシブ */
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
