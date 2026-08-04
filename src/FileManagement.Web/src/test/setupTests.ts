import '@testing-library/jest-dom/vitest'
import {
  cleanup,
} from '@testing-library/react'
import {
  afterEach,
  vi,
} from 'vitest'

afterEach(() => {
  cleanup()
})

Object.defineProperty(
  window,
  'matchMedia',
  {
    configurable: true,
    value: vi.fn().mockImplementation(
      (query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }),
    ),
  },
)

class ResizeObserverStub {
  observe(): void {
  }

  unobserve(): void {
  }

  disconnect(): void {
  }
}

globalThis.ResizeObserver =
  ResizeObserverStub

const getComputedStyle =
  window.getComputedStyle.bind(window)

window.getComputedStyle = (
  element: Element,
) =>
  getComputedStyle(element)
