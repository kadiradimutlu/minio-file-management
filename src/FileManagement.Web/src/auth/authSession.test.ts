import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'
import type {
  AuthSession,
} from '../models/auth'
import {
  clearAuthSession,
  getAuthSession,
  saveAuthSession,
  subscribeToAuthSession,
} from './authSession'

const storageKey =
  'file-management.auth-session'

function createSession(
  expiresAtUtc =
    new Date(
      Date.now() + 60_000,
    ).toISOString(),
): AuthSession {
  return {
    accessToken: 'access-token',
    tokenType: 'Bearer',
    expiresAtUtc,
    userId: 'user-123',
    email: 'user@example.com',
    roles: ['User'],
  }
}

describe('authSession', () => {
  beforeEach(() => {
    sessionStorage.clear()
  })

  it('stores and reads a valid session', () => {
    const session =
      createSession()

    saveAuthSession(session)

    expect(
      getAuthSession(),
    ).toEqual(session)
  })

  it('publishes login and logout changes', () => {
    const listener = vi.fn()
    const unsubscribe =
      subscribeToAuthSession(
        listener,
      )

    const session =
      createSession()

    saveAuthSession(session)
    clearAuthSession('logout')

    expect(listener).toHaveBeenNthCalledWith(
      1,
      {
        session,
        reason: 'login',
      },
    )

    expect(listener).toHaveBeenNthCalledWith(
      2,
      {
        session: null,
        reason: 'logout',
      },
    )

    unsubscribe()
  })

  it('removes an expired session', () => {
    sessionStorage.setItem(
      storageKey,
      JSON.stringify(
        createSession(
          new Date(
            Date.now() - 60_000,
          ).toISOString(),
        ),
      ),
    )

    expect(
      getAuthSession(),
    ).toBeNull()

    expect(
      sessionStorage.getItem(
        storageKey,
      ),
    ).toBeNull()
  })

  it('removes malformed JSON', () => {
    sessionStorage.setItem(
      storageKey,
      '{invalid',
    )

    expect(
      getAuthSession(),
    ).toBeNull()

    expect(
      sessionStorage.getItem(
        storageKey,
      ),
    ).toBeNull()
  })

  it('stops publishing after unsubscribe', () => {
    const listener = vi.fn()
    const unsubscribe =
      subscribeToAuthSession(
        listener,
      )

    unsubscribe()
    saveAuthSession(
      createSession(),
    )

    expect(listener).not.toHaveBeenCalled()
  })
})
