import type {
  AxiosProgressEvent,
} from 'axios'
import {
  apiClient,
} from './httpClient'
import type {
  FileAccessUrl,
  RelatedRecordAssociation,
  StoredFile,
} from '../models/file'

export async function listFiles(
  association?: RelatedRecordAssociation,
): Promise<StoredFile[]> {
  const response =
    await apiClient.get<StoredFile[]>(
      '/files',
      {
        params: association,
      },
    )

  return response.data
}

export async function uploadFile(
  file: File,
  association?: RelatedRecordAssociation,
  onProgress?: (percent: number) => void,
): Promise<StoredFile> {
  const formData = new FormData()

  formData.append(
    'file',
    file,
  )

  if (association) {
    formData.append(
      'relatedRecordType',
      association.relatedRecordType,
    )

    formData.append(
      'relatedRecordId',
      association.relatedRecordId,
    )
  }

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
            (event.loaded * 100) /
              event.total,
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
  await apiClient.delete(
    `/files/${id}`,
  )
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

async function getFileContent(
  id: string,
  operation: 'download' | 'preview',
): Promise<Blob> {
  const response =
    await apiClient.get<Blob>(
      `/files/${id}/${operation}`,
      {
        responseType: 'blob',
      },
    )

  return response.data
}

export async function downloadFile(
  id: string,
): Promise<Blob> {
  return getFileContent(
    id,
    'download',
  )
}

export async function previewFile(
  id: string,
): Promise<Blob> {
  return getFileContent(
    id,
    'preview',
  )
}
