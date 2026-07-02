import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Video, Languages, MonitorUp, Disc, SlidersHorizontal, ShieldCheck, type LucideIcon,
} from 'lucide-react';
import { api } from '../api/client';
import type { LandingContentItem } from '../api/types';
import TopBar from '../components/TopBar';

const ICONS: Record<string, LucideIcon> = {
  video: Video,
  languages: Languages,
  'monitor-up': MonitorUp,
  disc: Disc,
  'sliders-horizontal': SlidersHorizontal,
  'shield-check': ShieldCheck,
};

interface FeatureItem { icon: string; title: string; text: string; }
interface HowItem { step: number; title: string; text: string; }

/** All copy on this page is CMS-driven — editable from the super admin panel. */
export default function Landing() {
  const { t, i18n } = useTranslation();
  const [content, setContent] = useState<Record<string, string>>({});

  useEffect(() => {
    api
      .get<LandingContentItem[]>(`/api/public/landing/${i18n.language}`)
      .then((items) => setContent(Object.fromEntries(items.map((c) => [c.sectionKey, c.content]))))
      .catch(() => setContent({}));
  }, [i18n.language]);

  const features = useMemo<FeatureItem[]>(() => {
    try { return JSON.parse(content['features.items'] ?? '[]'); } catch { return []; }
  }, [content]);

  const howSteps = useMemo<HowItem[]>(() => {
    try { return JSON.parse(content['how.items'] ?? '[]'); } catch { return []; }
  }, [content]);

  return (
    <>
      <TopBar />
      <main className="container">
        <section className="hero">
          <h1>{content['hero.title'] ?? 'SmartCall'}</h1>
          <p>{content['hero.subtitle'] ?? ''}</p>
          <Link to="/register" className="btn" style={{ fontSize: 17, padding: '14px 34px' }}>
            {content['hero.cta'] ?? t('landing.getStarted')}
          </Link>
        </section>

        {features.length > 0 && (
          <section>
            <h2 style={{ textAlign: 'center' }}>{content['features.title']}</h2>
            <div className="features-grid">
              {features.map((f, i) => {
                const Icon = ICONS[f.icon] ?? Video;
                return (
                  <div className="feature-card" key={i}>
                    <div className="icon"><Icon size={28} /></div>
                    <h3>{f.title}</h3>
                    <p>{f.text}</p>
                  </div>
                );
              })}
            </div>
          </section>
        )}

        {howSteps.length > 0 && (
          <section>
            <h2 style={{ textAlign: 'center' }}>{content['how.title']}</h2>
            <div className="how-steps">
              {howSteps.map((s) => (
                <div className="how-step" key={s.step}>
                  <div className="num">{s.step}</div>
                  <h3>{s.title}</h3>
                  <p style={{ color: 'var(--text-dim)', fontSize: 14 }}>{s.text}</p>
                </div>
              ))}
            </div>
          </section>
        )}

        <section className="cta-band">
          <h2>{content['cta.title'] ?? ''}</h2>
          <Link to="/register" className="btn" style={{ marginTop: 10 }}>
            {content['cta.button'] ?? t('landing.getStarted')}
          </Link>
        </section>
      </main>
      <footer>{content['footer.contact'] ?? ''}</footer>
    </>
  );
}
