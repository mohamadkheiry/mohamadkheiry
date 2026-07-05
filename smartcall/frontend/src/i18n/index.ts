import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import fa from './fa.json';
import en from './en.json';

const stored = localStorage.getItem('smartcall.lang');

i18n.use(initReactI18next).init({
  resources: { fa: { translation: fa }, en: { translation: en } },
  lng: stored ?? 'fa',
  fallbackLng: 'fa',
  interpolation: { escapeValue: false },
});

export function applyDirection(lang: string) {
  document.documentElement.lang = lang;
  document.documentElement.dir = lang === 'fa' ? 'rtl' : 'ltr';
}

applyDirection(i18n.language);

i18n.on('languageChanged', (lang) => {
  localStorage.setItem('smartcall.lang', lang);
  applyDirection(lang);
});

export default i18n;
