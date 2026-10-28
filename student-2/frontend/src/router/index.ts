import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'automations',
      component: () => import('@/views/AutomationsView.vue'),
    },
    {
      path: '/new',
      name: 'new-automation',
      component: () => import('@/views/AutomationFormView.vue'),
    },
    {
      path: '/:id/edit',
      name: 'edit-automation',
      component: () => import('@/views/AutomationFormView.vue'),
    },
    {
      path: '/history',
      name: 'history',
      component: () => import('@/views/RunHistoryView.vue'),
    },
    {
      path: '/runs/:id/review',
      name: 'quiz-review',
      component: () => import('@/views/QuizReviewView.vue'),
    },
  ],
})

export default router