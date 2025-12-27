<template>
  <div class="viewer-page">
    <header class="header">
      <button @click="goBack" class="back-button">← 戻る</button>
      <h2 class="title">{{ itemName }}</h2>
      <div class="spacer"></div>
    </header>

    <!-- Video -->
    <div class="video-viewer">
      <div class="video-container">
        <video class="video-player" :src="`/api/files/${props.position}/video`" controls autoplay muted playsinline preload="metadata"></video>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { FileType } from '../types';

const props = defineProps<{
  fileType: FileType;
  position: string;
}>();

onMounted(() => {
  console.log(props);
});

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

.header {
  position: sticky;
  top: 0;
  z-index: 100;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border);
  padding: 1rem 2rem;
  display: flex;
  align-items: center;
  gap: 1rem;
}

.back-button {
  padding: 0.5rem 1rem;
  background: var(--bg-card);
  border-radius: 8px;
  font-weight: 500;
  transition: all 0.2s;
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

.spacer {
  width: 100px;
}

.loading {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 60vh;
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

@media (max-width: 768px) {
  .header {
    padding: 1rem;
  }

  .title {
    font-size: 1rem;
  }

  .spacer {
    display: none;
  }
}

.video-viewer {
  min-height: calc(100vh - 80px);
  padding: 2rem;
}

.video-container {
  max-width: 1200px;
  margin: 0 auto;
  background: var(--bg-card);
  border-radius: 12px;
  overflow: hidden;
}

.video-player {
  width: 100%;
  display: block;
}

@media (max-width: 768px) {
  .video-viewer {
    padding: 1rem;
  }
}
</style>
