export const maximumFileSizeBytes =
  20 * 1024 * 1024

export const allowedExtensions = [
  '.pdf',
  '.png',
  '.jpg',
  '.jpeg',
  '.txt',
  '.docx',
  '.xlsx',
]

export function getExtension(
  fileName: string,
): string {
  const separatorIndex =
    fileName.lastIndexOf('.')

  return separatorIndex < 0
    ? ''
    : fileName
        .slice(separatorIndex)
        .toLowerCase()
}

export type UploadValidationResult =
  | {
      valid: true
    }
  | {
      valid: false
      errorMessage: string
    }

export function validateUploadFile(
  file: Pick<File, 'name' | 'size'>,
): UploadValidationResult {
  if (
    file.size >
    maximumFileSizeBytes
  ) {
    return {
      valid: false,
      errorMessage:
        `${file.name} 20 MB sınırını aşıyor.`,
    }
  }

  const extension =
    getExtension(file.name)

  if (
    !allowedExtensions.includes(
      extension,
    )
  ) {
    return {
      valid: false,
      errorMessage:
        `${file.name} desteklenen bir uzantıya sahip değil.`,
    }
  }

  return {
    valid: true,
  }
}
