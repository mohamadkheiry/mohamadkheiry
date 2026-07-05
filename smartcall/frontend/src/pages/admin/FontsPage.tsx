import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save } from 'lucide-react';
import { api } from '../../api/client';
import type { Typography } from '../../api/types';

const SCOPES = [
  { value: 0, label: 'Header' },
  { value: 1, label: 'Page title' },
  { value: 2, label: 'Body' },
  { value: 3, label: 'Buttons' },
  { value: 4, label: 'Captions' },
];

export default function FontsPage() {
  const { t } = useTranslation();
  const [typography, setTypography] = useState<Typography | null>(null);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);

  const load = () => {
    api.get<Typography>('/api/admin/typography').then(setTypography).catch((e) => setError(e.message));
  };

  useEffect(load, []);

  if (!typography) return <p>{t('common.loading')}</p>;

  const assignmentFor = (scope: number, lang: string) =>
    typography.assignments.find((a) => a.scope === scope && a.language === lang);

  const assign = async (scope: number, lang: string, fontId: string, sizePx: number) => {
    setSaved(false);
    setError('');
    try {
      await api.post('/api/admin/fonts/assign', { scope, language: lang, fontId, fontSizePx: sizePx });
      setSaved(true);
      load();
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    }
  };

  const renderLangSection = (lang: 'fa' | 'en', title: string) => {
    const fonts = typography.fonts.filter((f) => f.language === lang && f.isActive);
    return (
      <div className="card" style={{ marginBottom: 18 }}>
        <h3>{title}</h3>
        <table>
          <thead>
            <tr>
              <th>{t('admin.scope')}</th>
              <th>Font</th>
              <th>{t('admin.fontSize')}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {SCOPES.map((scope) => {
              const current = assignmentFor(scope.value, lang);
              return (
                <ScopeRow
                  key={scope.value}
                  scopeLabel={scope.label}
                  fonts={fonts}
                  currentFontId={current?.fontId ?? fonts[0]?.id ?? ''}
                  currentSize={current?.fontSizePx ?? 16}
                  onSave={(fontId, size) => assign(scope.value, lang, fontId, size)}
                />
              );
            })}
          </tbody>
        </table>
      </div>
    );
  };

  return (
    <div style={{ maxWidth: 820 }}>
      <h2>{t('admin.fonts')}</h2>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">{t('admin.saved')}</div>}
      {renderLangSection('fa', 'فارسی')}
      {renderLangSection('en', 'English')}
    </div>
  );
}

function ScopeRow({
  scopeLabel, fonts, currentFontId, currentSize, onSave,
}: {
  scopeLabel: string;
  fonts: { id: string; name: string; fontFamily: string }[];
  currentFontId: string;
  currentSize: number;
  onSave: (fontId: string, size: number) => void;
}) {
  const { t } = useTranslation();
  const [fontId, setFontId] = useState(currentFontId);
  const [size, setSize] = useState(currentSize);

  useEffect(() => {
    setFontId(currentFontId);
    setSize(currentSize);
  }, [currentFontId, currentSize]);

  const selected = fonts.find((f) => f.id === fontId);

  return (
    <tr>
      <td>{scopeLabel}</td>
      <td>
        <select value={fontId} onChange={(e) => setFontId(e.target.value)} style={{ minWidth: 160 }}>
          {fonts.map((f) => (
            <option key={f.id} value={f.id}>{f.name}</option>
          ))}
        </select>
        {selected && (
          <div style={{ fontFamily: selected.fontFamily, fontSize: size, marginTop: 6, color: 'var(--text-dim)' }}>
            نمونه متن — Sample text
          </div>
        )}
      </td>
      <td>
        <input type="number" min={8} max={96} value={size} onChange={(e) => setSize(Number(e.target.value))} style={{ width: 80 }} dir="ltr" />
      </td>
      <td>
        <button className="btn secondary" style={{ padding: '6px 12px', fontSize: 13 }} onClick={() => onSave(fontId, size)}>
          <Save size={14} />
          {t('admin.save')}
        </button>
      </td>
    </tr>
  );
}
