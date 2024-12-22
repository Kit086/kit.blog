import Link from 'next/link';
import type { Post } from '@/interfaces/Post';

interface PostCardProps {
  post: Post;
}

export default function PostCard({ post }: PostCardProps) {
  const formattedDate = new Date(post.create_time).toLocaleDateString('en-US', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  return (
    <div className="card bg-base-100 shadow-xl">
      <div className="card-body">
        <Link href={`/posts/${post.slug}`}>
          <h2 className="card-title hover:text-primary transition-colors">
            {post.title}
          </h2>
        </Link>
        <time className="text-sm text-base-content/70" dateTime={post.create_time}>
          {formattedDate}
        </time>
        <p className="mt-2">{post.description}</p>
        <div className="card-actions justify-start mt-4">
          {post.tags.map((tag) => (
            <div key={tag} className="badge badge-outline">{tag}</div>
          ))}
        </div>
      </div>
    </div>
  );
}
