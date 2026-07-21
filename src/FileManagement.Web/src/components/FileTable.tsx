import {
  CopyOutlined,
  DeleteOutlined,
  DownloadOutlined,
  EyeOutlined,
  FileOutlined,
} from '@ant-design/icons'
import {
  Button,
  Empty,
  Popconfirm,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import type {
  TableColumnsType,
} from 'antd'
import {
  getDownloadUrl,
  getPreviewUrl,
} from '../api/fileApi'
import type {
  StoredFile,
} from '../models/file'

const { Text } = Typography

interface FileTableProps {
  files: StoredFile[]
  loading: boolean
  onCopyLink: (
    id: string,
  ) => Promise<void>
  onDelete: (
    id: string,
  ) => Promise<void>
}

function formatFileSize(
  sizeBytes: number,
): string {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`
  }

  const units = [
    'KB',
    'MB',
    'GB',
  ]

  let size = sizeBytes / 1024
  let unitIndex = 0

  while (
    size >= 1024 &&
    unitIndex < units.length - 1
  ) {
    size /= 1024
    unitIndex += 1
  }

  return `${size.toFixed(1)} ${units[unitIndex]}`
}

function supportsPreview(
  contentType: string,
): boolean {
  return (
    contentType === 'application/pdf' ||
    contentType.startsWith('image/')
  )
}

function startDownload(
  url: string,
): void {
  const link =
    document.createElement('a')

  link.href = url
  link.rel = 'noopener'

  document.body.appendChild(link)
  link.click()
  link.remove()
}

export function FileTable({
  files,
  loading,
  onCopyLink,
  onDelete,
}: FileTableProps) {
  const columns:
    TableColumnsType<StoredFile> = [
      {
        title: 'Dosya',
        dataIndex: 'originalFileName',
        key: 'originalFileName',
        render: (
          fileName: string,
        ) => (
          <Space>
            <FileOutlined />

            <Text
              ellipsis={{
                tooltip: fileName,
              }}
              strong
            >
              {fileName}
            </Text>
          </Space>
        ),
      },
      {
        title: 'Tür',
        dataIndex: 'contentType',
        key: 'contentType',
        responsive: ['md'],
        render: (
          contentType: string,
        ) => (
          <Tag>
            {contentType}
          </Tag>
        ),
      },
      {
        title: 'Boyut',
        dataIndex: 'sizeBytes',
        key: 'sizeBytes',
        width: 110,
        render: (
          sizeBytes: number,
        ) => formatFileSize(sizeBytes),
      },
      {
        title: 'Yüklenme',
        dataIndex: 'createdAtUtc',
        key: 'createdAtUtc',
        width: 180,
        responsive: ['lg'],
        render: (
          createdAtUtc: string,
        ) =>
          new Intl.DateTimeFormat(
            'tr-TR',
            {
              dateStyle: 'medium',
              timeStyle: 'short',
            },
          ).format(
            new Date(createdAtUtc),
          ),
      },
      {
        title: 'İşlemler',
        key: 'actions',
        width: 210,
        fixed: 'right',
        render: (
          _,
          file,
        ) => (
          <Space size="small">
            <Tooltip title="Önizle">
              <Button
                aria-label="Dosyayı önizle"
                disabled={
                  !supportsPreview(
                    file.contentType,
                  )
                }
                icon={<EyeOutlined />}
                onClick={() => {
                  window.open(
                    getPreviewUrl(file.id),
                    '_blank',
                    'noopener,noreferrer',
                  )
                }}
              />
            </Tooltip>

            <Tooltip title="İndir">
              <Button
                aria-label="Dosyayı indir"
                icon={<DownloadOutlined />}
                onClick={() => {
                  startDownload(
                    getDownloadUrl(file.id),
                  )
                }}
              />
            </Tooltip>

            <Tooltip title="Süreli bağlantıyı kopyala">
              <Button
                aria-label="Süreli bağlantıyı kopyala"
                icon={<CopyOutlined />}
                onClick={() => {
                  void onCopyLink(file.id)
                }}
              />
            </Tooltip>

            <Popconfirm
              cancelText="Vazgeç"
              okButtonProps={{
                danger: true,
              }}
              okText="Sil"
              onConfirm={() =>
                onDelete(file.id)
              }
              title="Dosya silinsin mi?"
            >
              <Tooltip title="Sil">
                <Button
                  aria-label="Dosyayı sil"
                  danger
                  icon={<DeleteOutlined />}
                />
              </Tooltip>
            </Popconfirm>
          </Space>
        ),
      },
    ]

  return (
    <Table
      columns={columns}
      dataSource={files}
      loading={loading}
      locale={{
        emptyText: (
          <Empty
            description="Henüz dosya yüklenmedi"
          />
        ),
      }}
      pagination={{
        defaultPageSize: 10,
        hideOnSinglePage: true,
        showSizeChanger: true,
      }}
      rowKey="id"
      scroll={{
        x: 900,
      }}
    />
  )
}