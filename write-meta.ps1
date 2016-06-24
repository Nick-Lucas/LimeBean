param ([string]$build_number, [string]$tag)

$meta_version_numeric = "0.0"

if ($tag -match '^v?(([.\d]+)[\w-]*)$')  {
    $meta_version_full = $matches[1]
    $meta_version_numeric = $matches[2]
} elseif ($build_number) {
    $meta_version_full = "$meta_version_numeric-ci-build$build_number"
} else {
    $meta_version_full = $meta_version_numeric
}

$meta_name = "LimeBean-Revival"
$meta_description = "RedBeanPHP-inspired data access layer"
$meta_author = "Nick Lucas"
$meta_copyright = "Copyright (c) 2014-2016 Aleksey Martynov, 2016-$(get-date -format yyyy) $meta_author"
$meta_project_url = "https://github.com/Nick-Lucas/LimeBean-Revival"
$meta_license_url = "https://raw.githubusercontent.com/Nick-Lucas/LimeBean/master/LICENSE.txt"

("LimeBean\AssemblyInfo.cs", "LimeBean.NetCore\LimeBean\project.json") | %{
    $path = "$PSScriptRoot\$_"

    (Get-Content $path) | %{
        $_  -replace '(AssemblyVersion.+?")[^"]+', "`${1}$meta_version_numeric" `
            -replace '("version":.+?")[^"]+', "`${1}$meta_version_full" `
            -replace '%lime_name%', $meta_name `
            -replace '%lime_description%', $meta_description `
            -replace '%lime_author%', $meta_author `
            -replace '%lime_copyright%', $meta_copyright `
            -replace '%lime_project_url%', $meta_project_url `
            -replace '%lime_license_url%', $meta_license_url
    } | Set-Content $path
}
