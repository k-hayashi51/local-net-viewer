import { ImageShowMode } from '../types';

const positionKey = 'position';
const imageShowModeKey = 'imageShowMode';
const pdfQualityScaleKey = 'pdfQualityScale';

export const setPosition = (position: string) => {
  setStorage(positionKey, position);
};

export const getPosition = (): string => {
  return getStorage(positionKey) ?? '';
};

export const setImageShowMode = (imageShowMode: ImageShowMode) => {
  setStorage(imageShowModeKey, imageShowMode);
};

export const getImageShowMode = (): ImageShowMode => {
  return getStorage(imageShowModeKey) ?? ImageShowMode.Scroll;
};

export const setPdfQualityScale = (pdfQualityScale: number) => {
  setStorage(pdfQualityScaleKey, pdfQualityScale);
};

export const getPdfQualityScale = (): number => {
  return getStorage(pdfQualityScaleKey) ?? 1.5;
};

const setStorage = <T>(key: string, value: T): void => {
  localStorage.setItem(key, JSON.stringify(value));
};

const getStorage = <T>(key: string): T | null => {
  const raw = localStorage.getItem(key);
  return raw ? (JSON.parse(raw) as T) : null;
};
