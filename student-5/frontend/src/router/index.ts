import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'overview',
      component: () => import('../views/GradesOverviewView.vue'),
    },
    {
      path: '/courses/:courseId',
      name: 'course',
      component: () => import('../views/CourseDetailView.vue'),
      props: true,
    },
  ],
})

export default router
