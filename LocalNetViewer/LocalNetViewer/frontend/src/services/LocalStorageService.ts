import { ImagePageMode } from "../types";

const positionKey = 'position';
const imagePageModeKey = 'imagePageMode';

export const setPosition = (position: string) => {
  setStorage(positionKey, position)
}

export const getPosition = (): string => {
  return getStorage(positionKey) ?? '';
}

export const setImagePageMode = (imagePageMode: ImagePageMode) => {
  setStorage(imagePageModeKey, imagePageMode)
}

export const getImagePageMode = (): ImagePageMode => {
  return getStorage(imagePageModeKey) ?? ImagePageMode.Scroll;
}

const setStorage = <T>(key: string, value: T): void => {
    localStorage.setItem(key, JSON.stringify(value));
}

const getStorage = <T>(key: string): T | null => {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : null;
}
