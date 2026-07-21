export interface StoredFile {
  id: string
  originalFileName: string
  contentType: string
  sizeBytes: number
  createdAtUtc: string
}

export interface FileAccessUrl {
  url: string
  expiresAtUtc: string
}