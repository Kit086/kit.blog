import Image from 'next/image';
import Link from 'next/link';
import ThemeSelector from './ThemeSelector';

export default function Header() {
  return (
    <header className="w-full">
      {/* 顶部栏 */}
      <div 
        className="w-full bg-cover bg-center h-24 relative"
        style={{ backgroundImage: 'url(/header.jpg)' }}
      >
        <div className="container mx-auto px-4 h-full flex items-center justify-between">
          {/* 左侧：头像和网站名称 */}
          <div className="flex items-center gap-4">
            <div className="relative w-16 h-16 rounded-full overflow-hidden">
              <Image
                src="/avatar.jpg"
                alt="Kit Lau"
                fill
                className="object-cover"
              />
            </div>
            <h1 className="text-2xl font-bold text-primary">
              Kit Lau&apos;s Cosmos
            </h1>
          </div>
          
          {/* 右侧：主题选择器 */}
          <ThemeSelector />
        </div>
      </div>

      {/* 导航栏 */}
      <nav className="bg-base-200">
        <div className="container mx-auto px-4">
          <ul className="flex gap-8 py-4">
            <li>
              <Link href="/" className="link link-hover">
                Home
              </Link>
            </li>
            <li>
              <Link href="/posts" className="link link-hover">
                Posts
              </Link>
            </li>
            <li>
              <Link href="/about" className="link link-hover">
                About
              </Link>
            </li>
          </ul>
        </div>
      </nav>
    </header>
  );
}
