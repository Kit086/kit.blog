import { getPostBySlug, getAllPostSlugs } from '@/lib/posts';
import { notFound } from 'next/navigation';
import type { Post } from '@/interfaces/Post';

interface Props {
  params: Promise<{
    slug: string;
  }>;
}

export async function generateStaticParams() {
  const slugs = getAllPostSlugs();
  return slugs.map((slug) => ({
    slug,
  }));
}

export default async function PostPage({ params }: Props) {
  try {
    const resolvedParams = await params;
    const post: Post = await getPostBySlug(resolvedParams.slug);

    return (
      <article className="prose lg:prose-xl mx-auto px-4">
        <h1>{post.title}</h1>
        <div className="text-sm text-gray-500">
          <time dateTime={post.create_time}>
            {new Date(post.create_time).toLocaleDateString()}
          </time>
          {post.tags.length > 0 && (
            <div className="mt-2">
              {post.tags.map((tag) => (
                <span key={tag} className="mr-2 px-2 py-1 bg-gray-100 rounded-md">
                  {tag}
                </span>
              ))}
            </div>
          )}
        </div>
        <div className="mt-8" dangerouslySetInnerHTML={{ __html: post.contentHtml || '' }} />
      </article>
    );
  } catch (error) {
    notFound();
  }
}
