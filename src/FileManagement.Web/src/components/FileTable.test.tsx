import {
  render,
  screen,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  describe,
  expect,
  it,
  vi,
} from 'vitest'
import type {
  StoredFile,
} from '../models/file'
import {
  FileTable,
} from './FileTable'

function createFile(
  contentType:
    string = 'application/pdf',
): StoredFile {
  return {
    id:
      '4b62950e-5ed0-4e09-a62b-b09c82b86dcb',
    originalFileName:
      'report.pdf',
    contentType,
    sizeBytes: 2048,
    relatedRecordType: null,
    relatedRecordId: null,
    createdAtUtc:
      '2026-08-04T08:00:00Z',
  }
}

function renderTable(
  file = createFile(),
) {
  const callbacks = {
    onCopyLink: vi.fn(),
    onDelete: vi.fn(),
    onDownload: vi.fn(),
    onPreview: vi.fn(),
  }

  render(
    <FileTable
      files={[file]}
      loading={false}
      {...callbacks}
    />,
  )

  return callbacks
}

describe('FileTable', () => {
  it('invokes download and preview actions', async () => {
    const user =
      userEvent.setup()

    const callbacks =
      renderTable()

    await user.click(
      screen.getByRole(
        'button',
        {
          name: 'Dosyayı indir',
        },
      ),
    )

    await user.click(
      screen.getByRole(
        'button',
        {
          name: 'Dosyayı önizle',
        },
      ),
    )

    expect(
      callbacks.onDownload,
    ).toHaveBeenCalledOnce()

    expect(
      callbacks.onPreview,
    ).toHaveBeenCalledOnce()
  })

  it('disables preview for unsupported content types', () => {
    renderTable(
      createFile('text/plain'),
    )

    expect(
      screen.getByRole(
        'button',
        {
          name: 'Dosyayı önizle',
        },
      ),
    ).toBeDisabled()
  })
})
