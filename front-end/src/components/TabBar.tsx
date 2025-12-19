import type { ReactNode } from 'react';

export type TabKey = 'search' | 'ai';

interface TabBarProps {
  activeTab: TabKey;
  onChange: (tab: TabKey) => void;
}

function cn(...classes: Array<string | false | null | undefined>) {
  return classes.filter(Boolean).join(' ');
}

const tabs: Array<{
  key: TabKey;
  label: string;
  icon: ReactNode;
}> = [
  {
    key: 'search',
    label: 'Tìm kiếm',
    icon: (
      <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
        />
      </svg>
    ),
  },
  {
    key: 'ai',
    label: 'Chat với A.I',
    icon: (
      <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M8 10h.01M12 10h.01M16 10h.01M21 12c0 4.418-4.03 8-9 8a9.78 9.78 0 01-4-.8L3 20l1.2-3.6A7.76 7.76 0 013 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
        />
      </svg>
    ),
  },
];

export default function TabBar({ activeTab, onChange }: TabBarProps) {
  return (
    <div className="inline-flex rounded-xl bg-white/60 p-1 backdrop-blur-md ring-1 ring-black/5 shadow-sm">
      {tabs.map((tab) => (
        <button
          key={tab.key}
          type="button"
          onClick={() => onChange(tab.key)}
          className={cn(
            'flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition',
            activeTab === tab.key
              ? 'bg-primary-600 text-white shadow-sm'
              : 'text-slate-700 hover:bg-white/70 hover:text-slate-900',
          )}
        >
          {tab.icon}
          <span>{tab.label}</span>
        </button>
      ))}
    </div>
  );
}
