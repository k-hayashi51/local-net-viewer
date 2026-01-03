import { createApp } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import App from './App.vue';
import ListView from './components/ListView.vue';
import ImagePage from './components/ImagePage.vue';
import PdfPage from './components/PdfPage.vue';
import VideoPage from './components/VideoPage.vue';
import './style.css';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: ListView,
      props: (route) => ({
        page: route.query.page as string | undefined,
        perPage: route.query.perPage as string | undefined,
      }),
    },
    {
      path: '/:position+',
      component: ListView,
      props: (route) => ({
        position: Array.isArray(route.params.position) ? route.params.position.join('-') : route.params.position,
        page: route.query.page as string | undefined,
        perPage: route.query.perPage as string | undefined,
      }),
    },
    { path: '/image/:position', component: ImagePage, props: true },
    { path: '/pdf/:position', component: PdfPage, props: true },
    { path: '/video/:position', component: VideoPage, props: true },
  ],
});

const app = createApp(App);
app.use(router);
app.mount('#app');
