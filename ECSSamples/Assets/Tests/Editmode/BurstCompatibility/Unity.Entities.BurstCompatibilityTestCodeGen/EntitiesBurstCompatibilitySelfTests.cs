using Unity.Collections;

namespace Unity.Entities.Tests
{
    [MayOnlyLiveInBlobStorage]
    public struct ShouldOnlyLiveInBlobStorage
    {
        public int i;
    }

    [GenerateTestsForBurstCompatibility]
    public struct BlobStorageRef
    {
        private static readonly BlobAssetReference<ShouldOnlyLiveInBlobStorage> Blob;

        public ref ShouldOnlyLiveInBlobStorage Property => ref Blob.Value;

        public static ref ShouldOnlyLiveInBlobStorage StaticMethod() => ref Blob.Value;

        public ref ShouldOnlyLiveInBlobStorage this[int i] => ref Blob.Value;

        public ref ShouldOnlyLiveInBlobStorage Method() => ref Blob.Value;
    }

    [GenerateTestsForBurstCompatibility]
    public struct BlobStorageRefReadonly
    {
        private static readonly BlobAssetReference<ShouldOnlyLiveInBlobStorage> Blob;

        public ref readonly ShouldOnlyLiveInBlobStorage Property => ref Blob.Value;

        public static ref readonly ShouldOnlyLiveInBlobStorage StaticMethod() => ref Blob.Value;

        public ref readonly ShouldOnlyLiveInBlobStorage this[int i] => ref Blob.Value;

        public ref readonly ShouldOnlyLiveInBlobStorage Method() => ref Blob.Value;
    }
}
