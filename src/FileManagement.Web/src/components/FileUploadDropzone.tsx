import { InboxOutlined } from '@ant-design/icons'
import {
  App,
  Input,
  Typography,
  Upload,
} from 'antd'
import type {
  UploadFile,
  UploadProps,
} from 'antd'
import { useState } from 'react'
import { uploadFile } from '../api/fileApi'
import type {
  RelatedRecordAssociation,
} from '../models/file'

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

  const [
    relatedRecordType,
    setRelatedRecordType,
  ] = useState('')

  const [
    relatedRecordId,
    setRelatedRecordId,
  ] = useState('')

  const hasValidAssociation = (): boolean => {
    const normalizedType =
      relatedRecordType.trim()

    const normalizedId =
      relatedRecordId.trim()

    const hasType =
      normalizedType.length > 0

    const hasId =
      normalizedId.length > 0

    if (hasType !== hasId) {
      void message.error(
        'İlgili kayıt türü ve ilgili kayıt kimliği birlikte doldurulmalıdır.',
      )

      return false
    }

    return true
  }

  const getAssociation =
    (): RelatedRecordAssociation | undefined => {
      const normalizedType =
        relatedRecordType.trim()

      const normalizedId =
        relatedRecordId.trim()

      if (
        normalizedType.length === 0 &&
        normalizedId.length === 0
      ) {
        return undefined
      }

      return {
        relatedRecordType:
          normalizedType,
        relatedRecordId:
          normalizedId,
      }
    }

  const beforeUpload:
    UploadProps['beforeUpload'] =
    (file) => {
      if (!hasValidAssociation()) {
        return Upload.LIST_IGNORE
      }

      if (
        file.size >
        maximumFileSizeBytes
      ) {
        void message.error(
          `${file.name} 20 MB sınırını aşıyor.`,
        )

        return Upload.LIST_IGNORE
      }

      const extension =
        getExtension(file.name)

      if (
        !allowedExtensions.includes(
          extension,
        )
      ) {
        void message.error(
          `${file.name} desteklenen bir uzantıya sahip değil.`,
        )

        return Upload.LIST_IGNORE
      }

      return true
    }

  const customRequest:
    NonNullable<
      UploadProps['customRequest']
    > =
    async ({
      file,
      onError,
      onProgress,
      onSuccess,
    }) => {
      const sourceFile =
        file as File

      try {
        const storedFile =
          await uploadFile(
            sourceFile,
            getAssociation(),
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
    NonNullable<
      UploadProps['onChange']
    > =
    ({
      fileList: nextFileList,
    }) => {
      setFileList(
        nextFileList.slice(-10),
      )
    }

  return (
    <>
      <div className="association-fields">
        <div className="field-group">
          <Text strong>
            İlgili kayıt türü
          </Text>

          <Input
            allowClear
            maxLength={100}
            onChange={(event) => {
              setRelatedRecordType(
                event.target.value,
              )
            }}
            placeholder="Örnek: Student"
            value={relatedRecordType}
          />
        </div>

        <div className="field-group">
          <Text strong>
            İlgili kayıt kimliği
          </Text>

          <Input
            allowClear
            maxLength={255}
            onChange={(event) => {
              setRelatedRecordId(
                event.target.value,
              )
            }}
            placeholder="Örnek: 42 veya UUID"
            value={relatedRecordId}
          />
        </div>
      </div>

      <Paragraph
        className="association-help"
        type="secondary"
      >
        İlişkilendirme isteğe bağlıdır.
        Kullanılacaksa iki alan birlikte
        doldurulmalıdır. Seçilen bütün
        dosyalara aynı ilişki uygulanır.
      </Paragraph>

      <Dragger
        accept={
          allowedExtensions.join(',')
        }
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
          PDF, PNG, JPG, TXT, DOCX ve
          XLSX. Dosya başına en fazla
          20 MB.
        </Text>
      </Dragger>
    </>
  )
}
