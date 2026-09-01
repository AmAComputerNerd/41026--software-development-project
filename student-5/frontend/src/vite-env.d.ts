/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GRADES_API_BASE_URL?: string
  readonly VITE_STUDENT_ID?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
