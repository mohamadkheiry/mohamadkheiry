import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save, PlugZap, Loader2 } from 'lucide-react';
import { api } from '../../api/client';
import type { AiSettings } from '../../api/types';

export default function AiSettingsPage() {
  const { t } = useTranslation();
  const [settings, setSettings] = useState<AiSettings | null>(null);
  const [apiKey, setApiKey] = useState('');
  const [saved, setSaved] = useState(false);
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get<AiSettings>('/api/admin/ai-settings').then(setSettings).catch((e) => setError(e.message));
  }, []);

  if (!settings) return <p>{t('common.loading')}</p>;

  const save = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSaved(false);
    try {
      await api.put('/api/admin/ai-settings', {
        apiKey: apiKey || null,
        baseUrl: settings.baseUrl,
        sttModel: settings.sttModel,
        translationModel: settings.translationModel,
        ttsModel: settings.ttsModel,
        ttsVoice: settings.ttsVoice,
        realtimeModel: settings.realtimeModel,
        activeMethod: settings.activeMethod,
      });
      setSaved(true);
      setApiKey('');
      if (apiKey) setSettings({ ...settings, hasApiKey: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    }
  };

  const test = async () => {
    setTesting(true);
    setTestResult(null);
    try {
      const result = await api.post<{ success: boolean; message: string; availableModels: string[] }>('/api/admin/ai-settings/test');
      setTestResult(result);
    } catch (err) {
      setTestResult({ success: false, message: err instanceof Error ? err.message : 'failed' });
    } finally {
      setTesting(false);
    }
  };

  return (
    <div style={{ maxWidth: 680 }}>
      <h2>{t('admin.ai')}</h2>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">{t('admin.saved')}</div>}
      {testResult && (
        <div className={testResult.success ? 'success-box' : 'error-box'}>{testResult.message}</div>
      )}

      <form onSubmit={save} className="card">
        <div className="field">
          <label>{t('admin.apiKey')}</label>
          <input
            type="password"
            dir="ltr"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder={settings.hasApiKey ? t('admin.apiKeySet') : 'sk-…'}
          />
        </div>
        <div className="field">
          <label>{t('admin.baseUrl')}</label>
          <input dir="ltr" value={settings.baseUrl ?? ''} onChange={(e) => setSettings({ ...settings, baseUrl: e.target.value })} />
        </div>

        <div className="field">
          <label>{t('admin.activeMethod')}</label>
          <select value={settings.activeMethod} onChange={(e) => setSettings({ ...settings, activeMethod: e.target.value })}>
            <option value="cascade">{t('admin.cascade')}</option>
            <option value="realtime">{t('admin.realtime')}</option>
          </select>
        </div>

        <div className="grid-2">
          <div className="field">
            <label>{t('admin.sttModel')}</label>
            <input dir="ltr" value={settings.sttModel ?? ''} onChange={(e) => setSettings({ ...settings, sttModel: e.target.value })} />
          </div>
          <div className="field">
            <label>{t('admin.mtModel')}</label>
            <input dir="ltr" value={settings.translationModel ?? ''} onChange={(e) => setSettings({ ...settings, translationModel: e.target.value })} />
          </div>
          <div className="field">
            <label>{t('admin.ttsModel')}</label>
            <input dir="ltr" value={settings.ttsModel ?? ''} onChange={(e) => setSettings({ ...settings, ttsModel: e.target.value })} />
          </div>
          <div className="field">
            <label>{t('admin.ttsVoice')}</label>
            <input dir="ltr" value={settings.ttsVoice ?? ''} onChange={(e) => setSettings({ ...settings, ttsVoice: e.target.value })} />
          </div>
          <div className="field">
            <label>{t('admin.realtimeModel')}</label>
            <input dir="ltr" value={settings.realtimeModel ?? ''} onChange={(e) => setSettings({ ...settings, realtimeModel: e.target.value })} />
          </div>
        </div>

        <div style={{ display: 'flex', gap: 10 }}>
          <button className="btn" type="submit">
            <Save size={16} />
            {t('admin.save')}
          </button>
          <button className="btn secondary" type="button" onClick={test} disabled={testing}>
            {testing ? <Loader2 size={16} className="spin" /> : <PlugZap size={16} />}
            {testing ? t('admin.testing') : t('admin.test')}
          </button>
        </div>
      </form>
    </div>
  );
}
