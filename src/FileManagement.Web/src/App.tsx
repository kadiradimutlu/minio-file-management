import { CloudUploadOutlined } from '@ant-design/icons';
import { Button, Card, Space, Tag, Typography } from 'antd';
import './App.css';

const { Title, Paragraph, Text } = Typography;

function App() {
  return (
    <main className="app-shell">
      <Card className="app-card">
        <Space direction="vertical" size="large">
          <div>
            <Tag color="blue">MinIO File Management</Tag>
            <Title level={2}>Dosya Yönetim Modülü</Title>
            <Paragraph>
              React, TypeScript, Vite ve Ant Design frontend iskeleti
              başarıyla çalışıyor.
            </Paragraph>
          </div>

          <Space wrap>
            <Button type="primary" icon={<CloudUploadOutlined />}>
              Dosya Yükle
            </Button>

            <Button disabled>Dosyaları Listele</Button>
          </Space>

          <Text type="secondary">
            API ve gerçek dosya yükleme işlemleri sonraki aşamalarda
            bağlanacaktır.
          </Text>
        </Space>
      </Card>
    </main>
  );
}

export default App;
