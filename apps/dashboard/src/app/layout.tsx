import type { Metadata } from 'next'
import { Geist, Geist_Mono } from 'next/font/google'
import Link from 'next/link'
import './globals.css'

const geistSans = Geist({ variable: '--font-geist-sans', subsets: ['latin'] })
const geistMono = Geist_Mono({ variable: '--font-geist-mono', subsets: ['latin'] })

export const metadata: Metadata = {
  title: 'DevPulse — Dashboard',
  description: 'Intervention dashboard for DevPulse',
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}>
      <body className="min-h-full bg-slate-50 text-slate-900">
        <nav className="bg-slate-900 text-white px-6 py-3 flex items-center gap-8 shadow-md">
          <span className="font-semibold tracking-tight text-slate-100">
            DevPulse
          </span>
          <Link href="/draft" className="text-sm text-slate-300 hover:text-white transition-colors">
            Draft
          </Link>
          <Link href="/episodes" className="text-sm text-slate-300 hover:text-white transition-colors">
            Episodes
          </Link>
        </nav>
        <main className="max-w-5xl mx-auto px-6 py-8">
          {children}
        </main>
      </body>
    </html>
  )
}
