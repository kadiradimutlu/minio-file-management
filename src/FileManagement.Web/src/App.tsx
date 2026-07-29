import {
  ClearOutlined,
  CloudServerOutlined,
  FilterOutlined,
  LogoutOutlined,
  ReloadOutlined,
  SafetyCertificateOutlined,
  UserOutlined,
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
  isAxiosError,
} from 'axios'
import {
  useCallback,
  useEffect,
  useState,
} from 'react'
import {
  login,
} from './api/authApi'
import {
  createPresignedUrl,
  deleteFile,
  downloadFile,
  listFiles,
  previewFile,
} from './api/fileApi'
import {
  clearAuthSession,
  getAuthSession,
  saveAuthSession,
  subscribeToAuthSession,
} from './auth/authSession'
import './App.css'
import {
  FileTable,
} from './components/FileTable'
import {
  FileUploadDropzone,
} from './components/FileUploadDropzone'
import {
  LoginScreen,
} from './components/LoginScreen'
import type {
  LoginFormValues,
} from './components/LoginScreen'
import type {
  AuthSession,
} from './models/auth'
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

function isUnauthorizedError(
  error: unknown,
): boolean {
  return (
    isAxiosError(error) &&
    error.response?.status === 401
  )
}

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

interface FileManagementAppProps {
  session: AuthSession
  onLogout: () => void
}

function FileManagementApp({
  session,
  onLogout,
}: FileManagementAppProps) {
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
      } catch (error) {
        if (!isUnauthorizedError(error)) {
          void message.error(
            'Dosya listesi alınamadı.',
          )
        }
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
      } catch (error) {
        if (!isUnauthorizedError(error)) {
          void message.error(
            'Dosya silinemedi.',
          )
        }
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
      } catch (error) {
        if (!isUnauthorizedError(error)) {
          void message.error(
            'Erişim bağlantısı oluşturulamadı.',
          )
        }
      }
    }

  const handleDownload =
    async (
      file: StoredFile,
    ): Promise<void> => {
      try {
        const blob =
          await downloadFile(file.id)

        const objectUrl =
          URL.createObjectURL(blob)

        const link =
          document.createElement('a')

        link.href = objectUrl
        link.download =
          file.originalFileName
        link.rel = 'noopener'

        document.body.appendChild(link)
        link.click()
        link.remove()

        window.setTimeout(
          () => {
            URL.revokeObjectURL(
              objectUrl,
            )
          },
          1000,
        )
      } catch (error) {
        if (!isUnauthorizedError(error)) {
          void message.error(
            'Dosya indirilemedi.',
          )
        }
      }
    }

  const handlePreview =
    async (
      file: StoredFile,
    ): Promise<void> => {
      const previewWindow =
        window.open(
          'about:blank',
          '_blank',
        )

      if (!previewWindow) {
        void message.warning(
          'Önizleme için açılır pencereye izin verin.',
        )

        return
      }

      previewWindow.opener = null

      try {
        const blob =
          await previewFile(file.id)

        const objectUrl =
          URL.createObjectURL(blob)

        previewWindow.location.replace(
          objectUrl,
        )

        window.setTimeout(
          () => {
            URL.revokeObjectURL(
              objectUrl,
            )
          },
          60_000,
        )
      } catch (error) {
        previewWindow.close()

        if (!isUnauthorizedError(error)) {
          void message.error(
            'Dosya önizlenemedi.',
          )
        }
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
        <Flex
          align="center"
          className="header-content header-row"
          gap={16}
          justify="space-between"
          wrap
        >
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

          <Space
            className="user-summary"
            size="middle"
            wrap
          >
            <Tag
              color="blue"
              icon={
                <SafetyCertificateOutlined />
              }
            >
              {session.roles.join(' · ')}
            </Tag>

            <Space size={6}>
              <UserOutlined />

              <Text>
                {session.email}
              </Text>
            </Space>

            <Button
              icon={
                <LogoutOutlined />
              }
              onClick={onLogout}
            >
              Çıkış
            </Button>
          </Space>
        </Flex>
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
                onDownload={
                  handleDownload
                }
                onPreview={
                  handlePreview
                }
              />
            </Flex>
          </Card>
        </div>
      </Content>

      <Footer className="app-footer">
        MinIO · ASP.NET Core ·
        PostgreSQL · React · JWT
      </Footer>
    </Layout>
  )
}

function App() {
  const { message } =
    AntApp.useApp()

  const [
    session,
    setSession,
  ] = useState<AuthSession | null>(
    () => getAuthSession(),
  )

  const [
    loginLoading,
    setLoginLoading,
  ] = useState(false)

  useEffect(
    () =>
      subscribeToAuthSession(
        (detail) => {
          setSession(detail.session)

          if (
            detail.reason ===
            'expired'
          ) {
            void message.warning(
              'Oturumunuz sona erdi. Lütfen yeniden giriş yapın.',
            )
          }
        },
      ),
    [message],
  )

  useEffect(() => {
    if (!session) {
      return
    }

    const expirationTime =
      Date.parse(session.expiresAtUtc)

    const remainingMilliseconds =
      expirationTime - Date.now()

    if (
      !Number.isFinite(
        remainingMilliseconds,
      ) ||
      remainingMilliseconds <= 0
    ) {
      clearAuthSession('expired')

      return
    }

    const timerId =
      window.setTimeout(
        () => {
          clearAuthSession(
            'expired',
          )
        },
        remainingMilliseconds,
      )

    return () => {
      window.clearTimeout(timerId)
    }
  }, [session])

  const handleLogin =
    async (
      values: LoginFormValues,
    ): Promise<void> => {
      setLoginLoading(true)

      try {
        const loginResult =
          await login({
            email: values.email.trim(),
            password: values.password,
          })

        saveAuthSession(loginResult)

        void message.success(
          `Hoş geldiniz, ${loginResult.email}`,
        )
      } catch (error) {
        if (
          isAxiosError(error) &&
          error.response?.status === 401
        ) {
          void message.error(
            'E-posta adresi veya parola hatalı.',
          )
        } else {
          void message.error(
            'Oturum açılamadı. Servis bağlantısını kontrol edin.',
          )
        }
      } finally {
        setLoginLoading(false)
      }
    }

  const handleLogout =
    (): void => {
      clearAuthSession('logout')

      void message.success(
        'Oturum kapatıldı.',
      )
    }

  if (!session) {
    return (
      <LoginScreen
        loading={loginLoading}
        onSubmit={handleLogin}
      />
    )
  }

  return (
    <FileManagementApp
      onLogout={handleLogout}
      session={session}
    />
  )
}

export default App
