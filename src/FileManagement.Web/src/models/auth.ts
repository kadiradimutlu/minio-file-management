export interface LoginRequest {
  email: string
  password: string
}

export interface AuthSession {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
  userId: string
  email: string
  roles: string[]
}
