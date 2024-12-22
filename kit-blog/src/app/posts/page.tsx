import { getAllPosts } from '@/lib/posts';
import PostCard from '@/components/PostCard';
import { Suspense } from 'react';

export default function PostsPage() {
  let posts = [];
  let error = null;

  try {
    posts = getAllPosts();
  } catch (e) {
    error = e.message;
    console.error('Error loading posts:', e);
  }

  if (error) {
    return (
      <div className="space-y-8">
        <h1 className="text-4xl font-bold">All Posts</h1>
        <div className="alert alert-error">
          <span>Error loading posts: {error}</span>
        </div>
      </div>
    );
  }
  
  return (
    <div className="space-y-8">
      <h1 className="text-4xl font-bold">All Posts</h1>
      <Suspense fallback={<div>Loading...</div>}>
        <div className="grid gap-8">
          {posts.map((post) => (
            <PostCard key={post.slug} post={post} />
          ))}
        </div>
      </Suspense>
    </div>
  );
}
