import {
  ClearOutlined,
  CloudServerOutlined,
  FilterOutlined,
  ReloadOutlined,
} from '@ant-design/icons'
import {
  App as AntApp,
  Button,
  Card,
  Col,
  Flex,
  Input,
  Layout,
  Row,
  Space,
  Statistic,
  Tag,
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
  RelatedRecordAssociation,
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

  document.body.appendChild(
    textArea,
  )

  textArea.select()
  document.execCommand('copy')
  textArea.remove()
}

function App() {
  const { message } =
    AntApp.useApp()

  const [files, setFiles] =
    useState<StoredFile[]>([])

  const [loading, setLoading] =
    useState(true)

  const [
    filterRecordType,
    setFilterRecordType,
  ] = useState('')

  const [
    filterRecordId,
    setFilterRecordId,
  ] = useState('')

  const [
    activeAssociation,
    setActiveAssociation,
  ] = useState<
    RelatedRecordAssociation | undefined
  >()

  const loadFiles =
    useCallback(async () => {
      setLoading(true)

      try {
        const result =
          await listFiles(
            activeAssociation,
          )

        setFiles(result)
      } catch {
        void message.error(
          'Dosya listesi alınamadı.',
        )
      } finally {
        setLoading(false)
      }
    }, [
      activeAssociation,
      message,
    ])

  useEffect(() => {
    void loadFiles()
  }, [loadFiles])

  const handleApplyFilter =
    (): void => {
      const normalizedType =
        filterRecordType.trim()

      const normalizedId =
        filterRecordId.trim()

      const hasType =
        normalizedType.length > 0

      const hasId =
        normalizedId.length > 0

      if (!hasType && !hasId) {
        setActiveAssociation(
          undefined,
        )

        return
      }

      if (hasType !== hasId) {
        void message.warning(
          'Filtre için ilgili kayıt türü ve kimliği birlikte doldurulmalıdır.',
        )

        return
      }

      setActiveAssociation({
        relatedRecordType:
          normalizedType,
        relatedRecordId:
          normalizedId,
      })
    }

  const handleClearFilter =
    (): void => {
      setFilterRecordType('')
      setFilterRecordId('')
      setActiveAssociation(
        undefined,
      )
    }

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
          await createPresignedUrl(
            id,
          )

        await copyText(
          accessUrl.url,
        )

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
      (
        total,
        file,
      ) =>
        total +
        file.sizeBytes,
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
                Güvenli ve yeniden
                kullanılabilir dosya
                yönetimi
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
              Dosyaları sürükleyip
              bırakın, PostgreSQL
              metadata bilgilerini
              görüntüleyin ve MinIO
              üzerinden indirin veya
              önizleyin.
            </Paragraph>
          </section>

          <Row gutter={[16, 16]}>
            <Col
              xs={24}
              md={12}
            >
              <Card>
                <Statistic
                  title="Gösterilen dosya"
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
                  title="Gösterilen toplam boyut"
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
              onUploaded={
                loadFiles
              }
            />
          </Card>

          <Card
            className="section-card"
            extra={
              <Button
                icon={
                  <ReloadOutlined />
                }
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
            <div className="filter-panel">
              <div className="filter-fields">
                <div className="field-group">
                  <Text strong>
                    İlgili kayıt türü
                  </Text>

                  <Input
                    allowClear
                    maxLength={100}
                    onChange={(event) => {
                      setFilterRecordType(
                        event.target.value,
                      )
                    }}
                    onPressEnter={
                      handleApplyFilter
                    }
                    placeholder="Örnek: Student"
                    value={
                      filterRecordType
                    }
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
                      setFilterRecordId(
                        event.target.value,
                      )
                    }}
                    onPressEnter={
                      handleApplyFilter
                    }
                    placeholder="Örnek: 42 veya UUID"
                    value={
                      filterRecordId
                    }
                  />
                </div>
              </div>

              <div className="filter-actions">
                <Button
                  icon={
                    <FilterOutlined />
                  }
                  onClick={
                    handleApplyFilter
                  }
                  type="primary"
                >
                  Filtrele
                </Button>

                <Button
                  disabled={
                    !activeAssociation &&
                    filterRecordType
                      .length === 0 &&
                    filterRecordId
                      .length === 0
                  }
                  icon={
                    <ClearOutlined />
                  }
                  onClick={
                    handleClearFilter
                  }
                >
                  Temizle
                </Button>

                {activeAssociation ? (
                  <Tag>
                    {
                      activeAssociation
                        .relatedRecordType
                    }
                    {' · '}
                    {
                      activeAssociation
                        .relatedRecordId
                    }
                  </Tag>
                ) : (
                  <Text type="secondary">
                    Tüm dosyalar
                  </Text>
                )}
              </div>
            </div>

            <Flex vertical>
              <FileTable
                files={files}
                loading={loading}
                onCopyLink={
                  handleCopyLink
                }
                onDelete={
                  handleDelete
                }
              />
            </Flex>
          </Card>
        </div>
      </Content>

      <Footer className="app-footer">
        MinIO · ASP.NET Core ·
        PostgreSQL · React
      </Footer>
    </Layout>
  )
}

export default App
