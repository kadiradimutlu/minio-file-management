import {
  App as AntApp,
  ConfigProvider,
} from 'antd'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App.tsx'
import './index.css'

const rootElement =
  document.getElementById('root')

if (!rootElement) {
  throw new Error(
    'Root element was not found.',
  )
}

createRoot(rootElement).render(
  <StrictMode>
    <ConfigProvider
      theme={{
        token: {
          borderRadius: 10,
        },
      }}
    >
      <AntApp>
        <App />
      </AntApp>
    </ConfigProvider>
  </StrictMode>,
)