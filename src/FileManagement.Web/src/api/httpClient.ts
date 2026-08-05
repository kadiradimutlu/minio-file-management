import axios from 'axios'
import type {
  AxiosError,
} from 'axios'
import {
  clearAuthSession,
  getAuthSession,
} from '../auth/authSession'

export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ??
  '/api'

export const apiClient =
  axios.create({
    baseURL: apiBaseUrl,
    timeout: 30_000,
  })

function isPublicAuthRequest(
  url: string | undefined,
): boolean {
  return (
    url?.endsWith('/auth/login') ===
      true ||
    url?.endsWith('/auth/register') ===
      true
  )
}

apiClient.interceptors.request.use(
  (config) => {
    const session =
      getAuthSession()

    if (session) {
      config.headers.set(
        'Authorization',
        `Bearer ${session.accessToken}`,
      )
    }

    return config
  },
)

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (
      error.response?.status === 401 &&
      !isPublicAuthRequest(
        error.config?.url,
      )
    ) {
      clearAuthSession('expired')
    }

    return Promise.reject(error)
  },
)
