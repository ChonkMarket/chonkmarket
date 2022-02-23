import React from 'react'
import { Spinner } from 'reactstrap'

export default function GraphSpinner() {

  return (
    <>
    {new Array(3).fill('').map((s, i) => (
      <Spinner type="grow" key={i} style={{color: 'var(--primary)'}}/>
    ))}
    </>
  )
}