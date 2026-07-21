import { InboxOutlined } from '@ant-design/icons'
import {
  App,
  Typography,
  Upload,
} from 'antd'
import type {
  UploadFile,
  UploadProps,
} from 'antd'
import { useState } from 'react'
import { uploadFile } from '../api/fileApi'

const { Dragger } = Upload
const { Paragraph, Text } = Typography

const maximumFileSizeBytes =
  20 * 1024 * 1024

const allowedExtensions = [
  '.pdf',
  '.png',
  '.jpg',
  '.jpeg',
  '.txt',
  '.docx',
  '.xlsx',
]

interface FileUploadDropzoneProps {
  onUploaded: () => Promise<void> | void
}

function getExtension(
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

export function FileUploadDropzone({
  onUploaded,
}: FileUploadDropzoneProps) {
  const { message } = App.useApp()

  const [fileList, setFileList] =
    useState<UploadFile[]>([])

  const beforeUpload: UploadProps['beforeUpload'] =
    (file) => {
      if (file.size > maximumFileSizeBytes) {
        void message.error(
          `${file.name} 20 MB sınırını aşıyor.`,
        )

        return Upload.LIST_IGNORE
      }

      const extension =
        getExtension(file.name)

      if (
        !allowedExtensions.includes(extension)
      ) {
        void message.error(
          `${file.name} desteklenen bir uzantıya sahip değil.`,
        )

        return Upload.LIST_IGNORE
      }

      return true
    }

  const customRequest:
    NonNullable<UploadProps['customRequest']> =
    async ({
      file,
      onError,
      onProgress,
      onSuccess,
    }) => {
      const sourceFile = file as File

      try {
        const storedFile = await uploadFile(
          sourceFile,
          (percent) => {
            onProgress?.({
              percent,
            })
          },
        )

        onSuccess?.(storedFile)

        void message.success(
          `${sourceFile.name} başarıyla yüklendi.`,
        )

        await onUploaded()
      } catch (error) {
        const uploadError =
          error instanceof Error
            ? error
            : new Error(
                'Dosya yükleme işlemi başarısız.',
              )

        onError?.(uploadError)

        void message.error(
          `${sourceFile.name} yüklenemedi.`,
        )
      }
    }

  const handleChange:
    NonNullable<UploadProps['onChange']> =
    ({ fileList: nextFileList }) => {
      setFileList(
        nextFileList.slice(-10),
      )
    }

  return (
    <Dragger
      accept={allowedExtensions.join(',')}
      beforeUpload={beforeUpload}
      customRequest={customRequest}
      fileList={fileList}
      maxCount={10}
      multiple
      onChange={handleChange}
      progress={{
        showInfo: true,
        strokeWidth: 3,
      }}
    >
      <p className="ant-upload-drag-icon">
        <InboxOutlined />
      </p>

      <Paragraph className="upload-title">
        Dosyaları buraya sürükleyin
        veya seçmek için tıklayın
      </Paragraph>

      <Text type="secondary">
        PDF, PNG, JPG, TXT, DOCX ve XLSX.
        Dosya başına en fazla 20 MB.
      </Text>
    </Dragger>
  )
}