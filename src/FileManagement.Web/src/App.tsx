import {
  CloudServerOutlined,
  ReloadOutlined,
} from '@ant-design/icons'
import {
  App as AntApp,
  Button,
  Card,
  Col,
  Flex,
  Layout,
  Row,
  Space,
  Statistic,
  Typography,
} from 'antd'
import {
  useCallback,
  useEffect,
  useState,
} from 'react'
import {
  createPresignedUrl,
  deleteFile,
  listFiles,
} from './api/fileApi'
import './App.css'
import {
  FileTable,
} from './components/FileTable'
import {
  FileUploadDropzone,
} from './components/FileUploadDropzone'
import type {
  StoredFile,
} from './models/file'

const {
  Content,
  Footer,
  Header,
} = Layout

const {
  Paragraph,
  Text,
  Title,
} = Typography

async function copyText(
  value: string,
): Promise<void> {
  if (navigator.clipboard) {
    await navigator.clipboard.writeText(
      value,
    )

    return
  }

  const textArea =
    document.createElement('textarea')

  textArea.value = value
  textArea.style.position = 'fixed'
  textArea.style.opacity = '0'

  document.body.appendChild(textArea)
  textArea.select()
  document.execCommand('copy')
  textArea.remove()
}

function App() {
  const { message } = AntApp.useApp()

  const [files, setFiles] =
    useState<StoredFile[]>([])

  const [loading, setLoading] =
    useState(true)

  const loadFiles =
    useCallback(async () => {
      setLoading(true)

      try {
        const result =
          await listFiles()

        setFiles(result)
      } catch {
        void message.error(
          'Dosya listesi alınamadı.',
        )
      } finally {
        setLoading(false)
      }
    }, [message])

  useEffect(() => {
    void loadFiles()
  }, [loadFiles])

  const handleDelete =
    async (
      id: string,
    ): Promise<void> => {
      try {
        await deleteFile(id)

        void message.success(
          'Dosya silindi.',
        )

        await loadFiles()
      } catch {
        void message.error(
          'Dosya silinemedi.',
        )
      }
    }

  const handleCopyLink =
    async (
      id: string,
    ): Promise<void> => {
      try {
        const accessUrl =
          await createPresignedUrl(id)

        await copyText(accessUrl.url)

        void message.success(
          'Beş dakikalık erişim bağlantısı kopyalandı.',
        )
      } catch {
        void message.error(
          'Erişim bağlantısı oluşturulamadı.',
        )
      }
    }

  const totalSizeBytes =
    files.reduce(
      (total, file) =>
        total + file.sizeBytes,
      0,
    )

  return (
    <Layout className="app-layout">
      <Header className="app-header">
        <div className="header-content">
          <Space size="middle">
            <div className="brand-icon">
              <CloudServerOutlined />
            </div>

            <div>
              <Title
                className="brand-title"
                level={3}
              >
                MinIO File Management
              </Title>

              <Text className="brand-subtitle">
                Güvenli ve yeniden kullanılabilir
                dosya yönetimi
              </Text>
            </div>
          </Space>
        </div>
      </Header>

      <Content className="app-content">
        <div className="content-container">
          <section className="intro-section">
            <Title level={2}>
              Dosyalarınızı yönetin
            </Title>

            <Paragraph type="secondary">
              Dosyaları sürükleyip bırakın,
              PostgreSQL metadata bilgilerini
              görüntüleyin ve MinIO üzerinden
              indirin veya önizleyin.
            </Paragraph>
          </section>

          <Row gutter={[16, 16]}>
            <Col
              xs={24}
              md={12}
            >
              <Card>
                <Statistic
                  title="Toplam dosya"
                  value={files.length}
                />
              </Card>
            </Col>

            <Col
              xs={24}
              md={12}
            >
              <Card>
                <Statistic
                  precision={2}
                  suffix="MB"
                  title="Toplam boyut"
                  value={
                    totalSizeBytes /
                    1024 /
                    1024
                  }
                />
              </Card>
            </Col>
          </Row>

          <Card
            className="section-card"
            title="Dosya yükle"
          >
            <FileUploadDropzone
              onUploaded={loadFiles}
            />
          </Card>

          <Card
            className="section-card"
            extra={
              <Button
                icon={<ReloadOutlined />}
                loading={loading}
                onClick={() => {
                  void loadFiles()
                }}
              >
                Yenile
              </Button>
            }
            title="Dosyalar"
          >
            <Flex vertical>
              <FileTable
                files={files}
                loading={loading}
                onCopyLink={
                  handleCopyLink
                }
                onDelete={handleDelete}
              />
            </Flex>
          </Card>
        </div>
      </Content>

      <Footer className="app-footer">
        MinIO · ASP.NET Core · PostgreSQL ·
        React
      </Footer>
    </Layout>
  )
}

export default App