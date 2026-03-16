function UnescapeJson($json) {
    foreach ($key in $json.PSObject.Properties.Name) {
        $value = $json.$key

        if ($value -is [string]) {
            try {
                $deserialized = ConvertFrom-Json $value
                $json.$key = $deserialized
                $value = $deserialized
            }
            catch {
                # If deserialization fails, do nothing
            }
        }

        if ($value -is [psobject]) {
            UnescapeJson $value
        }
        elseif ($value -is [array]) {
            foreach ($item in $value) {
                UnescapeJson $item
            }
        }
    }
}