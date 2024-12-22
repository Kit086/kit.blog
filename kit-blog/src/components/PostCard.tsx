import Link from 'next/link';
import { Post } from '@/interfaces/Post';

export default function PostCard({ post }: { post: Post }) {
  return (
    <div className="card bg-base-200 shadow-xl transition-all hover:shadow-2xl">
      <div className="card-body">
        <Link href={`/posts/${post.slug}`}>
          <h2 className="card-title text-2xl hover:text-primary/80 transition-colors">
            {post.title}
          </h2>
        </Link>
        <div className="text-sm text-base-content/70 space-y-2">
          <time dateTime={post.create_time}>
            {new Date(post.create_time).toLocaleDateString()}
          </time>
          {post.tags && post.tags.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {post.tags.map((tag) => (
                <span key={tag} className="badge badge-outline">
                  {tag}
                </span>
              ))}
            </div>
          )}
        </div>
        <p className="text-base-content/80 mt-4">{post.description}</p>
        <div className="card-actions justify-end mt-4">
          <Link 
            href={`/posts/${post.slug}`}
            className="btn btn-primary btn-sm"
          >
            阅读更多
          </Link>
        </div>
      </div>
    </div>
  );
}
