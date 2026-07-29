import {
  CloudServerOutlined,
  LockOutlined,
  MailOutlined,
  SafetyCertificateOutlined,
} from '@ant-design/icons'
import {
  Button,
  Card,
  Form,
  Input,
  Space,
  Tag,
  Typography,
} from 'antd'

const {
  Paragraph,
  Text,
  Title,
} = Typography

export interface LoginFormValues {
  email: string
  password: string
}

interface LoginScreenProps {
  loading: boolean
  onSubmit: (
    values: LoginFormValues,
  ) => Promise<void>
}

export function LoginScreen({
  loading,
  onSubmit,
}: LoginScreenProps) {
  return (
    <main className="login-layout">
      <div className="login-background" />

      <Card
        className="login-card"
        bordered={false}
      >
        <div className="login-brand-icon">
          <CloudServerOutlined />
        </div>

        <Space
          className="login-heading"
          direction="vertical"
          size={4}
        >
          <Title level={2}>
            MinIO File Management
          </Title>

          <Paragraph type="secondary">
            Dosya yönetim sistemine
            erişmek için hesabınızla
            oturum açın.
          </Paragraph>
        </Space>

        <Tag
          className="login-security-tag"
          color="blue"
          icon={
            <SafetyCertificateOutlined />
          }
        >
          JWT ile korunan erişim
        </Tag>

        <Form<LoginFormValues>
          layout="vertical"
          onFinish={(values) => {
            void onSubmit(values)
          }}
          requiredMark={false}
        >
          <Form.Item
            label="E-posta"
            name="email"
            rules={[
              {
                required: true,
                message:
                  'E-posta adresinizi girin.',
              },
              {
                type: 'email',
                message:
                  'Geçerli bir e-posta adresi girin.',
              },
            ]}
          >
            <Input
              autoComplete="username"
              placeholder="admin@filemanagement.local"
              prefix={
                <MailOutlined />
              }
              size="large"
            />
          </Form.Item>

          <Form.Item
            label="Parola"
            name="password"
            rules={[
              {
                required: true,
                message:
                  'Parolanızı girin.',
              },
            ]}
          >
            <Input.Password
              autoComplete="current-password"
              placeholder="Parolanız"
              prefix={
                <LockOutlined />
              }
              size="large"
            />
          </Form.Item>

          <Button
            block
            htmlType="submit"
            loading={loading}
            size="large"
            type="primary"
          >
            Oturum aç
          </Button>
        </Form>

        <Text
          className="login-session-note"
          type="secondary"
        >
          Oturum bilgileri yalnızca bu
          tarayıcı sekmesinde saklanır.
        </Text>
      </Card>
    </main>
  )
}
