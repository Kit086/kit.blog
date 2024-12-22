import type { Metadata } from 'next';
import localFont from "next/font/local";
import './globals.css';
import Header from '@/components/Header';
import Footer from '@/components/Footer';

const geistSans = localFont({
  src: "./fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
});
const geistMono = localFont({
  src: "./fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
});

export const metadata: Metadata = {
  title: "Kit Lau's Cosmos",
  description: 'A personal blog by Kit Lau',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="bg-base-100">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased min-h-screen bg-gradient-to-b from-base-200 to-base-100`}
      >
        <div className="min-h-screen flex flex-col bg-base-100/50 backdrop-blur-sm">
          <Header />
          <main className="flex-grow w-full">
            <div className="container mx-auto px-4 py-8 w-[95%] md:w-[80%]">
              {children}
            </div>
          </main>
          <Footer />
        </div>
      </body>
    </html>
  );
}
