import {
  apiClient,
} from './httpClient'
import type {
  AuthSession,
  LoginRequest,
} from '../models/auth'

export async function login(
  request: LoginRequest,
): Promise<AuthSession> {
  const response =
    await apiClient.post<AuthSession>(
      '/auth/login',
      request,
    )

  return response.data
}
