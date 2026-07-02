import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save } from 'lucide-react';
import { api } from '../../api/client';
import type { GeneralSettings } from '../../api/types';

export default function GeneralSettingsPage() {
  const { t } = useTranslation();
  const [settings, setSettings] = useState<GeneralSettings | null>(null);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get<GeneralSettings>('/api/admin/general-settings').then(setSettings).catch((e) => setError(e.message));
  }, []);

  if (!settings) return <p>{t('common.loading')}</p>;

  const save = async (e: FormEvent) => {
    e.preventDefault();
    setSaved(false);
    setError('');
    try {
      await api.put('/api/admin/general-settings', {
        defaultLanguage: settings.defaultLanguage,
        allowLanguageSwitch: settings.allowLanguageSwitch,
        iceServersJson: settings.iceServersJson,
      });
      setSaved(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    }
  };

  return (
    <div style={{ maxWidth: 680 }}>
      <h2>{t('admin.general')}</h2>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">{t('admin.saved')}</div>}
      <form onSubmit={save} className="card">
        <div className="field">
          <label>{t('admin.defaultLanguage')}</label>
          <select value={settings.defaultLanguage} onChange={(e) => setSettings({ ...settings, defaultLanguage: e.target.value })}>
            <option value="fa">فارسی</option>
            <option value="en">English</option>
          </select>
        </div>
        <div className="field" style={{ flexDirection: 'row', alignItems: 'center', gap: 10 }}>
          <input
            type="checkbox"
            checked={settings.allowLanguageSwitch}
            onChange={(e) => setSettings({ ...settings, allowLanguageSwitch: e.target.checked })}
            style={{ width: 'auto' }}
            id="allow-switch"
          />
          <label htmlFor="allow-switch" style={{ color: 'var(--text)' }}>{t('admin.allowSwitch')}</label>
        </div>
        <div className="field">
          <label>{t('admin.iceServers')}</label>
          <textarea
            rows={4}
            dir="ltr"
            value={settings.iceServersJson ?? ''}
            onChange={(e) => setSettings({ ...settings, iceServersJson: e.target.value })}
          />
        </div>
        <button className="btn" type="submit">
          <Save size={16} />
          {t('admin.save')}
        </button>
      </form>
    </div>
  );
}
