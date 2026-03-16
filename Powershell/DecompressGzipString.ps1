function DecompressGzipString {
    param (
        [Parameter(Mandatory=$true)]
        [string]$CompressedString
    )

    try {
        # Convert the compressed string into a byte array
        $compressedBytes = [System.Convert]::FromBase64String($CompressedString)

        # Prepare a stream for the compressed data and another for the decompressed data
        $compressedStream = New-Object System.IO.MemoryStream
        $compressedStream.Write($compressedBytes, 0, $compressedBytes.Length)
        $compressedStream.Position = 0
        $decompressionStream = New-Object System.IO.Compression.GzipStream($compressedStream, [System.IO.Compression.CompressionMode]::Decompress)
        $streamReader = New-Object System.IO.StreamReader($decompressionStream)

        # Read the decompressed string from the stream
        $decompressedString = $streamReader.ReadToEnd()

        # Cleanup streams
        $streamReader.Close()
        $decompressionStream.Close()
        $compressedStream.Close()

        return $decompressedString
    } catch {
        Write-Error "An error occurred during decompression: $_"
        return $null
    }
}

# Example Usage
# .\Decompress-GzipString.ps1 "H4sIALdhCGUA/12RS2+DMBCE/0rkcxJBHk3DjVck1JC0xRyqqrIsWBUrYJBtDmmU/951aNSWm/3NDN4dLuSZK5AmbKVRvDDEu4wISyLiLdbuZjUld5aUxCPZy1MYkCk58AbwGoFRojhNJ6koKg41KhR4Y414TIUu2KE1oBHgPQJdKNEZ0coBWK+vtfiUDT5uH1ivxtSG7PfwOLPKxMVg6Ke5BsVsRPZ1fSNBPmIZ9WmeYfiIkUQaUJLXmeGm12zftqe+u+1JHJQzXoNuuPwl6TFKdkkcseCN5Vn8OihLx3XmUeD+NUQ+jVFaOIvlzNnO3EfqbL2V66038wd3M4zLdjgSG2qz012nZC/kyZaL3d47xqLeP9BfibpkY3g80Fc/pMzvTdUq8QVlVlRQ9jWoH0veldxAGUsjzJl4yxGh5w7sEkM7/6TbbsPPvX4Dbuvdyh8CAAA="

