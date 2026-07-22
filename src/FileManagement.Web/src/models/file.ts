export interface RelatedRecordAssociation {
  relatedRecordType: string
  relatedRecordId: string
}

export interface StoredFile {
  id: string
  originalFileName: string
  contentType: string
  sizeBytes: number
  createdAtUtc: string
  relatedRecordType: string | null
  relatedRecordId: string | null
}

export interface FileAccessUrl {
  url: string
  expiresAtUtc: string
}
