import {
  describe,
  expect,
  it,
} from 'vitest'
import {
  getExtension,
  maximumFileSizeBytes,
  validateUploadFile,
} from './fileUploadValidation'

describe('FileUploadDropzone validation', () => {
  it('normalizes the file extension', () => {
    expect(
      getExtension('REPORT.PDF'),
    ).toBe('.pdf')

    expect(
      getExtension('README'),
    ).toBe('')
  })

  it('accepts a supported file at the size limit', () => {
    expect(
      validateUploadFile({
        name: 'report.pdf',
        size:
          maximumFileSizeBytes,
      }),
    ).toEqual({
      valid: true,
    })
  })

  it('rejects a file above the size limit', () => {
    expect(
      validateUploadFile({
        name: 'report.pdf',
        size:
          maximumFileSizeBytes + 1,
      }),
    ).toEqual({
      valid: false,
      errorMessage:
        'report.pdf 20 MB sınırını aşıyor.',
    })
  })

  it('rejects an unsupported extension', () => {
    expect(
      validateUploadFile({
        name: 'script.exe',
        size: 1024,
      }),
    ).toEqual({
      valid: false,
      errorMessage:
        'script.exe desteklenen bir uzantıya sahip değil.',
    })
  })
})
