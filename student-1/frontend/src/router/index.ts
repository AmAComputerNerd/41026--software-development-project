import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'notifications',
      component: () => import('../views/NotificationsView.vue'),
    },
    {
      path: '/preferences',
      name: 'preferences',
      component: () => import('../views/PreferencesView.vue'),
    },
    {
      path: '/digest',
      name: 'digest',
      component: () => import('../views/AiDigestView.vue'),
    },
  ],
})

export default router
