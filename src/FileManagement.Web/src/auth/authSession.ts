import type {
  AuthSession,
} from '../models/auth'

const storageKey =
  'file-management.auth-session'

const sessionEventName =
  'file-management:auth-session-changed'

export type AuthSessionChangeReason =
  | 'login'
  | 'logout'
  | 'expired'
  | 'invalid'

export interface AuthSessionChangeDetail {
  session: AuthSession | null
  reason: AuthSessionChangeReason
}

function isAuthSession(
  value: unknown,
): value is AuthSession {
  if (
    typeof value !== 'object' ||
    value === null
  ) {
    return false
  }

  const candidate =
    value as Partial<AuthSession>

  return (
    typeof candidate.accessToken ===
      'string' &&
    candidate.accessToken.length > 0 &&
    typeof candidate.tokenType ===
      'string' &&
    typeof candidate.expiresAtUtc ===
      'string' &&
    typeof candidate.userId ===
      'string' &&
    typeof candidate.email ===
      'string' &&
    Array.isArray(candidate.roles) &&
    candidate.roles.every(
      (role: unknown) =>
        typeof role === 'string',
    )
  )
}

function isExpired(
  session: AuthSession,
): boolean {
  const expirationTime =
    Date.parse(session.expiresAtUtc)

  return (
    !Number.isFinite(expirationTime) ||
    expirationTime <= Date.now()
  )
}

function notifySessionChange(
  detail: AuthSessionChangeDetail,
): void {
  window.dispatchEvent(
    new CustomEvent<AuthSessionChangeDetail>(
      sessionEventName,
      {
        detail,
      },
    ),
  )
}

export function getAuthSession():
  AuthSession | null {
  const serializedSession =
    sessionStorage.getItem(storageKey)

  if (!serializedSession) {
    return null
  }

  try {
    const parsedSession: unknown =
      JSON.parse(serializedSession)

    if (
      !isAuthSession(parsedSession) ||
      isExpired(parsedSession)
    ) {
      sessionStorage.removeItem(
        storageKey,
      )

      return null
    }

    return parsedSession
  } catch {
    sessionStorage.removeItem(storageKey)

    return null
  }
}

export function saveAuthSession(
  session: AuthSession,
): void {
  sessionStorage.setItem(
    storageKey,
    JSON.stringify(session),
  )

  notifySessionChange({
    session,
    reason: 'login',
  })
}

export function clearAuthSession(
  reason: AuthSessionChangeReason =
    'logout',
): void {
  sessionStorage.removeItem(storageKey)

  notifySessionChange({
    session: null,
    reason,
  })
}

export function subscribeToAuthSession(
  listener: (
    detail: AuthSessionChangeDetail,
  ) => void,
): () => void {
  const handleSessionChange = (
    event: Event,
  ): void => {
    const customEvent =
      event as CustomEvent<
        AuthSessionChangeDetail
      >

    listener(customEvent.detail)
  }

  window.addEventListener(
    sessionEventName,
    handleSessionChange,
  )

  return () => {
    window.removeEventListener(
      sessionEventName,
      handleSessionChange,
    )
  }
}
