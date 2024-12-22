import Link from 'next/link';

export default function Footer() {
  return (
    <footer className="w-full bg-base-200 py-6">
      <div className="container mx-auto px-4">
        <div className="flex flex-col md:flex-row justify-between items-center gap-4">
          <div className="text-lg font-semibold">
            Kit Lau&apos;s Cosmos
          </div>
          <nav>
            <ul className="flex gap-8">
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
          </nav>
        </div>
      </div>
    </footer>
  );
}
