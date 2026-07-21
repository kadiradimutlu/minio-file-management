import axios from 'axios'
import type { AxiosProgressEvent } from 'axios'
import type {
  FileAccessUrl,
  StoredFile,
} from '../models/file'

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? '/api'

const apiClient = axios.create({
  baseURL: apiBaseUrl,
  timeout: 30_000,
})

export async function listFiles(): Promise<StoredFile[]> {
  const response =
    await apiClient.get<StoredFile[]>('/files')

  return response.data
}

export async function uploadFile(
  file: File,
  onProgress?: (percent: number) => void,
): Promise<StoredFile> {
  const formData = new FormData()

  formData.append('file', file)

  const response =
    await apiClient.post<StoredFile>(
      '/files',
      formData,
      {
        onUploadProgress: (
          event: AxiosProgressEvent,
        ) => {
          if (!event.total) {
            return
          }

          const percent = Math.round(
            (event.loaded * 100) / event.total,
          )

          onProgress?.(percent)
        },
      },
    )

  return response.data
}

export async function deleteFile(
  id: string,
): Promise<void> {
  await apiClient.delete(`/files/${id}`)
}

export async function createPresignedUrl(
  id: string,
): Promise<FileAccessUrl> {
  const response =
    await apiClient.get<FileAccessUrl>(
      `/files/${id}/presigned-url`,
      {
        params: {
          expiresInMinutes: 5,
        },
      },
    )

  return response.data
}

function createFileUrl(
  id: string,
  operation: 'download' | 'preview',
): string {
  const normalizedBaseUrl =
    apiBaseUrl.replace(/\/$/, '')

  return `${normalizedBaseUrl}/files/${id}/${operation}`
}

export function getDownloadUrl(
  id: string,
): string {
  return createFileUrl(id, 'download')
}

export function getPreviewUrl(
  id: string,
): string {
  return createFileUrl(id, 'preview')
}