import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save } from 'lucide-react';
import { api } from '../../api/client';
import type { LandingContentItem } from '../../api/types';

/** Light CMS: edit every text/JSON block of the landing page per language. */
export default function LandingContentPage() {
  const { t } = useTranslation();
  const [lang, setLang] = useState<'fa' | 'en'>('fa');
  const [items, setItems] = useState<LandingContentItem[]>([]);
  const [edited, setEdited] = useState<Record<string, string>>({});
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get<LandingContentItem[]>(`/api/public/landing/${lang}`)
      .then((r) => {
        setItems(r.sort((a, b) => a.sectionKey.localeCompare(b.sectionKey)));
        setEdited({});
      })
      .catch((e) => setError(e.message));
  }, [lang]);

  const save = async (item: LandingContentItem) => {
    setSaved(false);
    setError('');
    try {
      await api.put('/api/admin/landing-content', {
        sectionKey: item.sectionKey,
        language: lang,
        content: edited[item.sectionKey] ?? item.content,
        mediaPath: item.mediaPath,
      });
      setSaved(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    }
  };

  return (
    <div style={{ maxWidth: 820 }}>
      <h2>{t('admin.landing')}</h2>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">{t('admin.saved')}</div>}

      <div className="field" style={{ maxWidth: 200 }}>
        <label>{t('common.language')}</label>
        <select value={lang} onChange={(e) => setLang(e.target.value as 'fa' | 'en')}>
          <option value="fa">فارسی</option>
          <option value="en">English</option>
        </select>
      </div>

      {items.map((item) => {
        const isJson = item.content.trim().startsWith('[') || item.content.trim().startsWith('{');
        return (
          <div className="card" key={item.id} style={{ marginBottom: 14 }}>
            <div className="field">
              <label dir="ltr">{item.sectionKey}</label>
              <textarea
                rows={isJson ? 8 : 2}
                dir={isJson ? 'ltr' : undefined}
                value={edited[item.sectionKey] ?? item.content}
                onChange={(e) => setEdited({ ...edited, [item.sectionKey]: e.target.value })}
                style={isJson ? { fontFamily: 'monospace', fontSize: 13 } : undefined}
              />
            </div>
            <button className="btn secondary" style={{ padding: '6px 14px', fontSize: 13 }} onClick={() => save(item)}>
              <Save size={14} />
              {t('admin.save')}
            </button>
          </div>
        );
      })}
    </div>
  );
}
